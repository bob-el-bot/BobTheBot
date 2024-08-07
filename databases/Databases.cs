using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Types;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Database
{
    public class BobEntities : DbContext
    {
        public virtual DbSet<Server> Server { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<NewsChannel> NewsChannel { get; set; }
        public virtual DbSet<BlackListUser> BlackListUser { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            // Parse the database URL
            var databaseUri = new Uri(databaseUrl);
            var userInfo = databaseUri.UserInfo.Split(':');

            var npgsqlConnectionString = new NpgsqlConnectionStringBuilder
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.AbsolutePath.TrimStart('/')
            }.ToString();

            optionsBuilder.UseNpgsql(npgsqlConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Server>()
                .Property(s => s.MaxQuoteLength)
                .HasDefaultValue(4096u); // Set default value for MaxQuoteLength

            modelBuilder.Entity<User>()
                .Property(u => u.ProfileColor)
                .HasDefaultValue("#2C2F33"); // Set default value for ProfileColor

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Retrieves the total size of the database in bytes.
        /// </summary>
        /// <returns>The size of the database in bytes.</returns>
        public async Task<double> GetDatabaseSizeBytes()
        {
            using var command = Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT pg_database_size(current_database());";
            await Database.OpenConnectionAsync();

            var result = await command.ExecuteScalarAsync();
            return Convert.ToDouble(result);
        }

        /// <summary>
        /// Retrieves the total number of entries in the entire database.
        /// </summary>
        /// <returns>The total number of entries in the database.</returns>
        public async Task<ulong> GetTotalEntries()
        {
            ulong totalCount = 0;

            foreach (var property in this.GetType().GetProperties())
            {
                if (property.PropertyType.IsGenericType &&
                    property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                {
                    var dbSet = property.GetValue(this) as IQueryable<object>;

                    ulong count = (ulong)await dbSet.LongCountAsync();

                    totalCount += count;
                }
            }

            return totalCount;
        }

        /// <summary>
        /// GetServer() returns a Server object using the Guild.Id as the key. If a server is not found in the database then a new entry is made and then returned.
        /// </summary>
        public async Task<Server> GetServer(ulong id)
        {
            var server = await Server.FindAsync(keyValues: id);
            if (server == null)
            {
                // Add server to DB
                await AddServer(new Server { Id = id });
                return await GetServer(id);
            }
            else
            {
                return server;
            }
        }

        /// <summary>
        /// UpdateServer() edits / overwrites an existing server in the database.
        /// </summary>
        public async Task UpdateServer(Server server)
        {
            Server.Update(server);
            await SaveChangesAsync();
        }

        /// <summary>
        /// AddServer() creates a new server entry in the database. 
        /// </summary>
        private async Task AddServer(Server server)
        {
            await Server.AddAsync(server);
            await SaveChangesAsync();
        }

        /// <summary>
        /// GetUser() returns a (non-Discord) User object using the User.Id as the key. If a user is not found in the database then a new entry is made and then returned.
        /// </summary>
        public async Task<User> GetUser(ulong id)
        {
            var user = await User.FindAsync(keyValues: id);
            if (user == null)
            {
                // Add server to DB
                await AddUser(new User { Id = id });
                return await GetUser(id);
            }
            else
            {
                return user;
            }
        }

        /// <summary>
        /// UpdateUser() edits / overwrites an existing user in the database.
        /// </summary>
        public async Task UpdateUser(User user)
        {
            User.Update(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// AddUser() creates a new user entry in the database. 
        /// </summary>
        private async Task AddUser(User user)
        {
            await User.AddAsync(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// GetUsers() retrieves a list of (non-Discord) User objects using their IDs as keys.
        /// </summary>
        /// <param name="ids">An array of user IDs.</param>
        /// <returns>A list of User objects.</returns>
        public async Task<List<User>> GetUsers(IEnumerable<ulong> ids)
        {
            var users = await User.Where(u => ids.Contains(u.Id)).ToListAsync();
            var missingIds = ids.Except(users.Select(u => u.Id));

            foreach (var id in missingIds)
            {
                // Add missing users to the database
                var newUser = new User { Id = id };
                await AddUser(newUser);
                users.Add(newUser);
            }

            return users;
        }

        /// <summary>
        /// UpdateUsers() edits / overwrites existing users in the database.
        /// </summary>
        /// <param name="users">A list of User objects to update.</param>
        public async Task UpdateUsers(IEnumerable<User> users)
        {
            foreach (var user in users)
            {
                User.Update(user);
            }

            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a news channel by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the news channel to retrieve.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains the retrieved <see cref="Types.NewsChannel"/>.
        /// </returns>
        public async Task<NewsChannel> GetNewsChannel(ulong id)
        {
            return await NewsChannel.FindAsync(keyValues: id);
        }

        /// <summary>
        /// Updates a news channel asynchronously.
        /// </summary>
        public async Task UpdateNewsChannel(NewsChannel newsChannel)
        {
            NewsChannel.Update(newsChannel);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Removes a news channel asynchronously.
        /// </summary>
        /// <param name="newsChannel">The news channel to be removed.</param>
        public async Task RemoveNewsChannel(NewsChannel newsChannel)
        {
            NewsChannel.Remove(newsChannel);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new news channel asynchronously.
        /// </summary>
        /// <param name="newsChannel">The news channel to be added.</param>
        public async Task AddNewsChannel(NewsChannel newsChannel)
        {
            await NewsChannel.AddAsync(newsChannel);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a blacklisted user by their unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the blacklisted user to retrieve.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains the retrieved <see cref="Types.BlackListUser"/>.
        /// </returns>
        public async Task<BlackListUser> GetUserFromBlackList(ulong id)
        {
            return await BlackListUser.FindAsync(keyValues: id);
        }

        /// <summary>
        /// Updates a blacklisted user asynchronously.
        /// </summary>
        /// <param name="user">The blacklisted user to update.</param>
        public async Task UpdateUserFromBlackList(BlackListUser user)
        {
            BlackListUser.Update(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Removes a user from the blacklist asynchronously.
        /// </summary>
        /// <param name="user">The blacklisted user to be removed.</param>
        public async Task RemoveUserFromBlackList(BlackListUser user)
        {
            BlackListUser.Remove(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a user to the blacklist asynchronously.
        /// </summary>
        /// <param name="user">The blacklisted user to be added.</param>
        public async Task AddUserToBlackList(BlackListUser user)
        {
            await BlackListUser.AddAsync(user);
            await SaveChangesAsync();
        }
    }
}
