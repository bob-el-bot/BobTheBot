using System.Threading.Tasks;
using Database.Types;
using Discord;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace Database
{
    public class BobEntities : DbContext
    {
        public virtual DbSet<Server> Server { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = "bob.db" };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);
            optionsBuilder.UseSqlite(connection);
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
                return await Server.FindAsync(keyValues: id);
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
        public async Task AddServer(Server server)
        {
            await Server.AddAsync(server);
            await SaveChangesAsync();
        }
    }
}
