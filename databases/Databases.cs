using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Types;
using Discord;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Database
{
    public class BobEntities : DbContext
    {
        public virtual DbSet<Server> Server { get; set; }
        public virtual DbSet<User> User { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = "bob.db" };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);
            optionsBuilder.UseSqlite(connection);
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
        public async Task AddUser(User user)
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
    }
}
