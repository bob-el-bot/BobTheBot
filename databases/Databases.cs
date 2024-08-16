using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Commands.Helpers;
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
        public virtual DbSet<ScheduledMessage> ScheduledMessage { get; set; }
        public virtual DbSet<ScheduledAnnouncement> ScheduledAnnouncement { get; set; }

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
        /// Retrieves a <see cref="Server"/> object by its unique identifier.
        /// If the server is not found, a new entry is created and returned.
        /// </summary>
        /// <param name="id">The unique identifier of the server.</param>
        /// <returns>
        /// A task representing the asynchronous operation. 
        /// The task result contains the retrieved or newly created <see cref="Server"/>.
        /// </returns>
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
        /// Updates an existing server asynchronously.
        /// </summary>
        /// <param name="server">The server to update.</param>
        public async Task UpdateServer(Server server)
        {
            Server.Update(server);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new server to the database asynchronously.
        /// </summary>
        /// <param name="server">The server to be added.</param>
        private async Task AddServer(Server server)
        {
            await Server.AddAsync(server);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a <see cref="User"/> object by its unique identifier.
        /// If the user is not found, a new entry is created and returned.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>
        /// A task representing the asynchronous operation. 
        /// The task result contains the retrieved or newly created <see cref="User"/>.
        /// </returns>
        public async Task<User> GetUser(ulong id)
        {
            var user = await User.FindAsync(keyValues: id);
            if (user == null)
            {
                // Add user to DB
                await AddUser(new User { Id = id });
                return await GetUser(id);
            }
            else
            {
                return user;
            }
        }

        /// <summary>
        /// Updates an existing user asynchronously.
        /// </summary>
        /// <param name="user">The user to update.</param>
        public async Task UpdateUser(User user)
        {
            User.Update(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new user to the database asynchronously.
        /// </summary>
        /// <param name="user">The user to be added.</param>
        private async Task AddUser(User user)
        {
            await User.AddAsync(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a list of <see cref="User"/> objects by their unique identifiers.
        /// If any user is not found, a new entry is created and returned for that user.
        /// </summary>
        /// <param name="ids">An array of user IDs.</param>
        /// <returns>
        /// A task representing the asynchronous operation. 
        /// The task result contains a list of retrieved or newly created <see cref="User"/> objects.
        /// </returns>
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
        /// Updates existing users asynchronously.
        /// </summary>
        /// <param name="users">A list of <see cref="User"/> objects to update.</param>
        public async Task UpdateUsers(IEnumerable<User> users)
        {
            foreach (var user in users)
            {
                User.Update(user);
            }

            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a <see cref="NewsChannel"/> by its unique identifier asynchronously.
        /// If the news channel is not found, <c>null</c> is returned.
        /// </summary>
        /// <param name="id">The unique identifier of the news channel to retrieve.</param>
        /// <returns>
        /// A task representing the asynchronous operation. 
        /// The task result contains the retrieved <see cref="NewsChannel"/> or <c>null</c> if not found.
        /// </returns>
        public async Task<NewsChannel> GetNewsChannel(ulong id)
        {
            return await NewsChannel.FindAsync(keyValues: id);
        }

        /// <summary>
        /// Updates an existing news channel asynchronously.
        /// </summary>
        /// <param name="newsChannel">The news channel to update.</param>
        public async Task UpdateNewsChannel(NewsChannel newsChannel)
        {
            NewsChannel.Update(newsChannel);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Removes a news channel from the database asynchronously.
        /// </summary>
        /// <param name="newsChannel">The news channel to be removed.</param>
        public async Task RemoveNewsChannel(NewsChannel newsChannel)
        {
            NewsChannel.Remove(newsChannel);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new news channel to the database asynchronously.
        /// </summary>
        /// <param name="newsChannel">The news channel to be added.</param>
        public async Task AddNewsChannel(NewsChannel newsChannel)
        {
            await NewsChannel.AddAsync(newsChannel);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a <see cref="BlackListUser"/> by their unique identifier asynchronously.
        /// If the blacklisted user is not found, <c>null</c> is returned.
        /// </summary>
        /// <param name="id">The unique identifier of the blacklisted user to retrieve.</param>
        /// <returns>
        /// A task representing the asynchronous operation. 
        /// The task result contains the retrieved <see cref="BlackListUser"/> or <c>null</c> if not found.
        /// </returns>
        public async Task<BlackListUser> GetUserFromBlackList(ulong id)
        {
            return await BlackListUser.FindAsync(keyValues: id);
        }

        /// <summary>
        /// Updates an existing blacklisted user asynchronously.
        /// </summary>
        /// <param name="user">The blacklisted user to update.</param>
        public async Task UpdateUserFromBlackList(BlackListUser user)
        {
            BlackListUser.Update(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Removes a blacklisted user from the database asynchronously.
        /// </summary>
        /// <param name="user">The blacklisted user to be removed.</param>
        public async Task RemoveUserFromBlackList(BlackListUser user)
        {
            BlackListUser.Remove(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new blacklisted user to the database asynchronously.
        /// </summary>
        /// <param name="user">The blacklisted user to be added.</param>
        public async Task AddUserToBlackList(BlackListUser user)
        {
            await BlackListUser.AddAsync(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a scheduled message by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the scheduled message to retrieve.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains the retrieved <see cref="Types.ScheduledMessage"/>.
        /// </returns>
        public async Task<ScheduledMessage> GetScheduledMessage(ulong id)
        {
            return await ScheduledMessage.FindAsync(keyValues: id);
        }

        /// <summary>
        /// Updates an existing scheduled message asynchronously.
        /// </summary>
        /// <param name="message">The scheduled message to update.</param>
        public async Task UpdateScheduledMessage(ScheduledMessage message)
        {
            ScheduledMessage.Update(message);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Removes a scheduled message from the database asynchronously.
        /// </summary>
        /// <param name="messageId">The unique identifier of the scheduled message to be removed.</param>
        public async Task RemoveScheduledMessage(ulong messageId)
        {
            await Database.ExecuteSqlRawAsync("DELETE FROM \"ScheduledMessage\" WHERE \"Id\" = @p0", messageId);
        }

        /// <summary>
        /// Adds a new scheduled message to the database asynchronously.
        /// </summary>
        /// <param name="message">The scheduled message to be added.</param>
        public async Task AddScheduledMessage(ScheduledMessage message)
        {
            await ScheduledMessage.AddAsync(message);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a scheduled announcement by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the scheduled announcement.</param>
        /// <returns>The scheduled announcement with the specified ID, or null if not found.</returns>
        public async Task<ScheduledAnnouncement> GetScheduledAnnouncement(ulong id)
        {
            return await ScheduledAnnouncement.FindAsync(keyValues: id);
        }

        /// <summary>
        /// Updates an existing scheduled announcement in the database.
        /// </summary>
        /// <param name="announcement">The scheduled announcement to update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateScheduledAnnouncement(ScheduledAnnouncement announcement)
        {
            ScheduledAnnouncement.Update(announcement);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Removes a scheduled announcement from the database by its unique identifier.
        /// </summary>
        /// <param name="announcementId">The unique identifier of the scheduled announcement to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RemoveScheduledAnnouncement(ulong announcementId)
        {
            await Database.ExecuteSqlRawAsync("DELETE FROM \"ScheduledAnnouncement\" WHERE \"Id\" = @p0", announcementId);
        }

        /// <summary>
        /// Adds a new scheduled announcement to the database.
        /// </summary>
        /// <param name="announcement">The scheduled announcement to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddScheduledAnnouncement(ScheduledAnnouncement announcement)
        {
            await ScheduledAnnouncement.AddAsync(announcement);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Removes a scheduled item (either a message or an announcement) from the database.
        /// </summary>
        /// <param name="item">The scheduled item to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RemoveScheduledItem(IScheduledItem item)
        {
            switch (item)
            {
                case ScheduledMessage message:
                    await RemoveScheduledMessage(message.Id);
                    break;
                case ScheduledAnnouncement announcement:
                    await RemoveScheduledAnnouncement(announcement.Id);
                    break;
            }
        }
    }
}
