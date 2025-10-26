using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database.Types;
using Bob.Database.Types.DataTransferObjects;
using BobTheBot.Chat.MemoryHandling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using Pgvector;


namespace Bob.Database
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BobEntities>
    {
        public BobEntities CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BobEntities>();
            var npgsqlConnectionString = DatabaseUtils.GetNpgsqlConnectionString();
            optionsBuilder.UseNpgsql(npgsqlConnectionString, o => o.UseVector());

            return new BobEntities(optionsBuilder.Options);
        }
    }

    public class BobEntities(DbContextOptions<BobEntities> options) : DbContext(options)
    {
        public virtual DbSet<Server> Server { get; set; }
        public virtual DbSet<WelcomeImage> WelcomeImage { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<NewsChannel> NewsChannel { get; set; }
        public virtual DbSet<BlackListUser> BlackListUser { get; set; }
        public virtual DbSet<ScheduledMessage> ScheduledMessage { get; set; }
        public virtual DbSet<ScheduledAnnouncement> ScheduledAnnouncement { get; set; }
        public virtual DbSet<ReactBoardMessage> ReactBoardMessage { get; set; }
        public virtual DbSet<Memory> Memory { get; set; }
        public DbSet<MemoryDTO> MemoryDTOs { get; set; }   // Only for projection queries
        public virtual DbSet<Tag> Tag { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var npgsqlConnectionString = DatabaseUtils.GetNpgsqlConnectionString();
                optionsBuilder.UseNpgsql(npgsqlConnectionString, o => o.UseVector());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Server>()
                .Property(s => s.MaxQuoteLength)
                .HasDefaultValue(4096u);

            modelBuilder.Entity<User>()
                .Property(u => u.ProfileColor)
                .HasDefaultValue("#2C2F33");

            modelBuilder.HasPostgresExtension("vector");
            modelBuilder.Entity<Memory>()
                .Property(m => m.Embedding)
                .HasColumnType("vector(1536)");

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasIndex(e => new { e.GuildId, e.AuthorId });
                entity.HasIndex(e => new { e.GuildId, e.Name }).IsUnique();
            });

            modelBuilder.Entity<ReactBoardMessage>(entity =>
            {
                entity.HasIndex(e => e.GuildId);
            });

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Retrieves the total size of the database in bytes.
        /// </summary>
        /// <returns>The size of the database in bytes.</returns>
        public virtual async Task<double> GetDatabaseSizeBytes()
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
        public virtual async Task<ulong> GetTotalEntries()
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
        /// Retrieves a <see cref="Server"/> by its unique identifier.
        /// If not found, creates, saves, and returns a new <see cref="Server"/> tracked by the context.
        /// </summary>
        /// <param name="id">The unique identifier of the server.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the existing or newly created <see cref="Server"/> tracked by the context.
        /// </returns>
        public virtual async Task<Server> GetOrCreateServerAsync(ulong id)
        {
            var server = await Server.FirstOrDefaultAsync(s => s.Id == id);
            if (server != null)
                return server;

            server = new Server { Id = id };
            Server.Add(server);
            await SaveChangesAsync();
            return server;
        }

        /// <summary>
        /// Retrieves a <see cref="Server"/> by its unique identifier.
        /// If not found, returns a new unsaved <see cref="Server"/> instance with the given ID (not tracked by the context).
        /// </summary>
        /// <param name="id">The unique identifier of the server.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the existing <see cref="Server"/> from the database, or a new unsaved instance if not found.
        /// </returns>
        public virtual async Task<Server> GetServerOrNew(ulong id)
        {
            var server = await Server.FirstOrDefaultAsync(s => s.Id == id);
            return server ?? new Server { Id = id };
        }

        /// <summary>
        /// Retrieves a <see cref="Server"/> by its unique identifier from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the server.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the <see cref="Server"/> if found, or <c>null</c> if not found.
        /// </returns>
        public virtual async Task<Server> GetServer(ulong id)
        {
            return await Server.FirstOrDefaultAsync(s => s.Id == id);
        }

        /// <summary>
        /// Inserts a new <see cref="Server"/> or updates an existing one by its unique identifier,
        /// applying the specified update action, and saves changes asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the server.</param>
        /// <param name="updateAction">An action to update the <see cref="Server"/> object.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the upserted <see cref="Server"/> entity.
        /// </returns>
        public virtual async Task<Server> UpsertServerAsync(ulong id, Action<Server> updateAction)
        {
            var server = await Server.FirstOrDefaultAsync(s => s.Id == id);
            if (server == null)
            {
                server = new Server { Id = id };
                updateAction(server);
                Server.Add(server);
            }
            else
            {
                updateAction(server);
            }
            await SaveChangesAsync();
            return server;
        }

        /// <summary>
        /// Removes the specified <see cref="Server"/> from the database and saves changes asynchronously.
        /// </summary>
        /// <param name="server">The <see cref="Server"/> to be removed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task RemoveServer(Server server)
        {
            Server.Remove(server);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a <see cref="User"/> by its unique identifier.
        /// If not found, creates, saves, and returns a new <see cref="User"/> tracked by the context.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the existing or newly created <see cref="User"/> tracked by the context.
        /// </returns>
        public virtual async Task<User> GetOrCreateUserAsync(ulong id)
        {
            var user = await User.FirstOrDefaultAsync(u => u.Id == id);
            if (user != null)
                return user;

            user = new User { Id = id };
            User.Add(user);
            await SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Retrieves a <see cref="User"/> by its unique identifier.
        /// If not found, returns a new unsaved <see cref="User"/> instance with the given ID (not tracked by the context).
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the existing <see cref="User"/> from the database, or a new unsaved instance if not found.
        /// </returns>
        public virtual async Task<User> GetUserOrNew(ulong id)
        {
            var user = await User.FirstOrDefaultAsync(u => u.Id == id);
            return user ?? new User { Id = id };
        }

        /// <summary>
        /// Retrieves a <see cref="User"/> by its unique identifier from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the <see cref="User"/> if found, or <c>null</c> if not found.
        /// </returns>
        public virtual async Task<User> GetUser(ulong id)
        {
            return await User.FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <summary>
        /// Inserts a new <see cref="User"/> or updates an existing one by its unique identifier,
        /// applying the specified update action, and saves changes asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <param name="updateAction">An action to update the <see cref="User"/> object.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the upserted <see cref="User"/> entity.
        /// </returns>
        public virtual async Task<User> UpsertUserAsync(ulong id, Action<User> updateAction)
        {
            var user = await User.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                user = new User { Id = id };
                updateAction(user);
                User.Add(user);
            }
            else
            {
                updateAction(user);
            }
            await SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Retrieves a list of <see cref="User"/> objects by their unique identifiers.
        /// For any missing users, creates and saves new <see cref="User"/> entries with those IDs.
        /// </summary>
        /// <param name="ids">A collection of user IDs.</param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains a list of all found and newly created <see cref="User"/> objects.
        /// </returns>
        public virtual async Task<List<User>> GetOrCreateUsersAsync(IEnumerable<ulong> ids)
        {
            var users = await User.Where(u => ids.Contains(u.Id)).ToListAsync();
            var existingIds = users.Select(u => u.Id).ToHashSet();
            var newUsers = ids.Except(existingIds).Select(id => new User { Id = id }).ToList();

            if (newUsers.Count > 0)
            {
                User.AddRange(newUsers);
                await SaveChangesAsync();
                users.AddRange(newUsers);
            }

            return users;
        }

        /// <summary>
        /// Updates or adds the specified <see cref="User"/> entities in the database and saves changes asynchronously.
        /// For each user, adds it if it does not exist, or updates it if it does.
        /// </summary>
        /// <param name="users">A collection of <see cref="User"/> objects to update or add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task UpdateOrAddUsers(IEnumerable<User> users)
        {
            foreach (var user in users)
            {
                var entry = Entry(user);
                if (entry.State == EntityState.Detached)
                {
                    bool exists = await User.AnyAsync(u => u.Id == user.Id);
                    if (exists)
                        User.Attach(user);
                    else
                        User.Add(user);
                }
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
        public virtual async Task<NewsChannel> GetNewsChannel(ulong id)
        {
            return await NewsChannel.FirstOrDefaultAsync(n => n.Id == id);
        }

        /// <summary>
        /// Removes a news channel from the database asynchronously.
        /// </summary>
        /// <param name="newsChannel">The news channel to be removed.</param>
        public virtual async Task RemoveNewsChannel(NewsChannel newsChannel)
        {
            NewsChannel.Remove(newsChannel);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new news channel to the database asynchronously.
        /// </summary>
        /// <param name="newsChannel">The news channel to be added.</param>
        public virtual async Task AddNewsChannel(NewsChannel newsChannel)
        {
            NewsChannel.Add(newsChannel);
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
        public virtual async Task<BlackListUser> GetUserFromBlackList(ulong id)
        {
            return await BlackListUser.FirstOrDefaultAsync(b => b.Id == id);
        }

        /// <summary>
        /// Inserts a new <see cref="BlackListUser"/> or updates an existing one by its unique identifier,
        /// applies the specified update action, and saves changes asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the user to upsert.</param>
        /// <param name="updateAction">
        /// An action to apply updates to the <see cref="BlackListUser"/> object.
        /// This action is invoked whether the user is newly created or already exists.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous operation.
        /// The task result contains the upserted <see cref="BlackListUser"/> entity.
        /// </returns>
        public virtual async Task<BlackListUser> UpsertBlackListUserAsync(ulong id, Action<BlackListUser> updateAction)
        {
            var user = await BlackListUser.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                user = new BlackListUser { Id = id };
                updateAction(user);
                BlackListUser.Add(user);
            }
            else
            {
                updateAction(user);
            }

            await SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Removes a blacklisted user from the database asynchronously.
        /// </summary>
        /// <param name="user">The blacklisted user to be removed.</param>
        public virtual async Task RemoveUserFromBlackList(BlackListUser user)
        {
            BlackListUser.Remove(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new blacklisted user to the database asynchronously.
        /// </summary>
        /// <param name="user">The blacklisted user to be added.</param>
        public virtual async Task AddUserToBlackList(BlackListUser user)
        {
            BlackListUser.Add(user);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a scheduled message by its unique identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the scheduled message to retrieve.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains the retrieved <see cref="Types.ScheduledMessage"/>.
        /// </returns>
        public virtual async Task<ScheduledMessage> GetScheduledMessage(ulong id)
        {
            return await ScheduledMessage.FirstOrDefaultAsync(m => m.Id == id);
        }

        /// <summary>
        /// Removes a scheduled message from the database asynchronously.
        /// </summary>
        /// <param name="messageId">The unique identifier of the scheduled message to be removed.</param>
        public virtual async Task RemoveScheduledMessage(ulong messageId)
        {
            await Database.ExecuteSqlRawAsync("DELETE FROM \"ScheduledMessage\" WHERE \"Id\" = @p0", messageId);
        }

        /// <summary>
        /// Adds a new scheduled message to the database asynchronously.
        /// </summary>
        /// <param name="message">The scheduled message to be added.</param>
        public virtual async Task AddScheduledMessage(ScheduledMessage message)
        {
            ScheduledMessage.Add(message);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a scheduled announcement by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the scheduled announcement.</param>
        /// <returns>The scheduled announcement with the specified ID, or null if not found.</returns>
        public virtual async Task<ScheduledAnnouncement> GetScheduledAnnouncement(ulong id)
        {
            return await ScheduledAnnouncement.FirstOrDefaultAsync(a => a.Id == id);
        }

        /// <summary>
        /// Removes a scheduled announcement from the database by its unique identifier.
        /// </summary>
        /// <param name="announcementId">The unique identifier of the scheduled announcement to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task RemoveScheduledAnnouncement(ulong announcementId)
        {
            await Database.ExecuteSqlRawAsync("DELETE FROM \"ScheduledAnnouncement\" WHERE \"Id\" = @p0", announcementId);
        }

        /// <summary>
        /// Adds a new scheduled announcement to the database.
        /// </summary>
        /// <param name="announcement">The scheduled announcement to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task AddScheduledAnnouncement(ScheduledAnnouncement announcement)
        {
            ScheduledAnnouncement.Add(announcement);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Removes a scheduled item (either a message or an announcement) from the database.
        /// </summary>
        /// <param name="item">The scheduled item to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task RemoveScheduledItem(IScheduledItem item)
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

        /// <summary>
        /// Retrieves a welcome image from the database by its ID.
        /// </summary>
        /// <param name="id">The unique ID of the welcome image (usually the server ID).</param>
        /// <returns>The corresponding <see cref="WelcomeImage"/> object if found; otherwise, null.</returns>
        public virtual async Task<WelcomeImage> GetWelcomeImage(ulong id)
        {
            return await WelcomeImage.FirstOrDefaultAsync(w => w.Id == id);
        }

        /// <summary>
        /// Removes a welcome image from the database.
        /// </summary>
        /// <param name="image">The <see cref="WelcomeImage"/> entity to remove.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        public virtual async Task RemoveWelcomeImage(WelcomeImage image)
        {
            WelcomeImage.Remove(image);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new welcome image to the database.
        /// </summary>
        /// <param name="image">The <see cref="WelcomeImage"/> entity to add.</param>
        /// <returns>A task that represents the asynchronous insert operation.</returns>
        public virtual async Task AddWelcomeImage(WelcomeImage image)
        {
            WelcomeImage.Add(image);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new ReactBoardMessage entry to the database and saves changes asynchronously.
        /// </summary>
        /// <param name="message">The ReactBoardMessage entity to add.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task AddReactBoardMessageAsync(ReactBoardMessage message)
        {
            ReactBoardMessage.Add(message);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Removes a ReactBoardMessage entry from the database and saves changes asynchronously.
        /// </summary>
        /// <param name="message">The ReactBoardMessage entity to remove.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task RemoveReactBoardMessageAsync(ReactBoardMessage message)
        {
            ReactBoardMessage.Remove(message);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a ReactBoardMessage entry by its original message ID asynchronously.
        /// </summary>
        /// <param name="originalMessageId">The original message ID to search for.</param>
        /// <returns>
        /// A task representing the asynchronous operation, with the ReactBoardMessage entity if found; otherwise, null.
        /// </returns>
        public virtual async Task<ReactBoardMessage> GetReactBoardMessageAsync(ulong originalMessageId)
        {
            return await ReactBoardMessage.FirstOrDefaultAsync(r => r.OriginalMessageId == originalMessageId);
        }

        /// <summary>
        /// Adds a dummy ReactBoardMessage entry for a guild if no entries exist yet.
        /// This prevents unnecessary Discord fetches for new React Board setups.
        /// </summary>
        /// <param name="guildId">The unique identifier of the guild.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task AddInitialReactBoardMessageAsync(ulong guildId)
        {
            bool anyExist = await ReactBoardMessage.AnyAsync(x => x.GuildId == guildId);

            if (anyExist)
            {
                return;
            }

            var dummyMessage = new ReactBoardMessage
            {
                OriginalMessageId = guildId,
                GuildId = guildId
            };

            await AddReactBoardMessageAsync(dummyMessage);
        }

        /// <summary>
        /// Adds a collection of ReactBoardMessage entries to the database for a specific guild and saves changes asynchronously.
        /// </summary>
        /// <param name="messages">The collection of ReactBoardMessage entities to add. Each should have the GuildId property set appropriately.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task AddMultipleReactBoardMessagesAsync(List<ReactBoardMessage> messages)
        {
            ReactBoardMessage.AddRange(messages);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves all ReactBoardMessage entries for a specific guild asynchronously.
        /// </summary>
        /// <param name="guildId">The unique identifier of the guild.</param>
        /// <returns>
        /// A task representing the asynchronous operation, with a list of ReactBoardMessage entities for the specified guild.
        /// </returns>
        public virtual async Task<List<ReactBoardMessage>> GetAllReactBoardMessagesForGuild(ulong guildId)
        {
            return await ReactBoardMessage.Where(x => x.GuildId == guildId).ToListAsync();
        }

        /// <summary>
        /// Removes all ReactBoardMessage entries for a specific guild from the database and saves changes asynchronously.
        /// </summary>
        /// <param name="guildId">The unique identifier of the guild whose ReactBoardMessages should be removed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task RemoveAllReactBoardMessagesForGuildAsync(ulong guildId)
        {
            var messages = await ReactBoardMessage
                .Where(x => x.GuildId == guildId)
                .ToListAsync();

            ReactBoardMessage.RemoveRange(messages);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Stores a new memory record for a user with the given content and embedding.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="userMessage">The message content to store.</param>
        /// <param name="botResponse">The bot's response to the user message.</param>
        /// <param name="embedding">The vector embedding of the content.</param>
        public async Task StoreMemoryAsync(string userId, string userMessage, string botResponse, Vector embedding)
        {
            bool ephemeral = false;
            if (!string.IsNullOrWhiteSpace(botResponse))
            {
                var lower = botResponse.ToLowerInvariant();
                ephemeral = lower.Contains("last conversation was") ||
                            lower.Contains("today") ||
                            lower.Contains("yesterday") ||
                            lower.Contains("this morning") ||
                            lower.Contains("this evening");
            }

            // Remove stale time‑sensitive rows for this user
            if (ephemeral)
            {
                var oldEphemeral = Memory.Where(m => m.UserId == userId && m.Ephemeral);
                Memory.RemoveRange(oldEphemeral);
            }

            var memory = new Memory
            {
                UserId = userId,
                UserMessage = userMessage,
                BotResponse = botResponse,
                Embedding = embedding,
                CreatedAt = DateTime.UtcNow,
                Ephemeral = ephemeral
            };

            Memory.Add(memory);
            await SaveChangesAsync();
        }

        public async Task<HybridMemoryResult> GetHybridMemoriesAsync(
            string userId,
            Vector queryEmbedding,
            DateTime? from = null,
            DateTime? to = null,
            int semanticLimit = 5,
            int temporalLimit = 5)
        {
            List<Memory> semanticMemories = [];
            List<Memory> temporalMemories = [];

            if (from.HasValue && to.HasValue)
            {
                var f = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
                var t = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);

                const string sql = @"
            SELECT ""Id"", ""UserId"", ""CreatedAt"",
                   ""UserMessage"", ""BotResponse"", ""Ephemeral""
            FROM ""Memory""
            WHERE ""UserId"" = @userId
              AND ""CreatedAt"" BETWEEN @from AND @to
              AND (""Ephemeral"" = FALSE OR ""CreatedAt"" > NOW() - INTERVAL '2 days')
            ORDER BY ""Embedding"" <-> @embedding
            LIMIT @limit;";

                var semanticDto = await MemoryDTOs
                    .FromSqlRaw(sql,
                        new NpgsqlParameter("userId", userId),
                        new NpgsqlParameter("embedding", queryEmbedding),
                        new NpgsqlParameter("from", f),
                        new NpgsqlParameter("to", t),
                        new NpgsqlParameter("limit", semanticLimit))
                    .AsNoTracking()
                    .ToListAsync();

                semanticMemories = [.. semanticDto.Select(d => new Memory
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    CreatedAt = d.CreatedAt,
                    UserMessage = d.UserMessage,
                    BotResponse = d.BotResponse,
                    Ephemeral = d.Ephemeral
                })];

                temporalMemories = await Memory
                    .Where(m => m.UserId == userId &&
                                m.CreatedAt >= f &&
                                m.CreatedAt <= t &&
                                (!m.Ephemeral || m.CreatedAt > DateTime.UtcNow.AddDays(-2)))
                    .OrderBy(m => m.CreatedAt)
                    .Take(temporalLimit)
                    .Select(m => new Memory
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        CreatedAt = m.CreatedAt,
                        UserMessage = m.UserMessage,
                        BotResponse = m.BotResponse,
                        Ephemeral = m.Ephemeral
                    })
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                const string sql = @"
            SELECT ""Id"", ""UserId"", ""CreatedAt"",
                   ""UserMessage"", ""BotResponse"", ""Ephemeral""
            FROM ""Memory""
            WHERE ""UserId"" = @userId
              AND (""Ephemeral"" = FALSE OR ""CreatedAt"" > NOW() - INTERVAL '2 days')
            ORDER BY ""Embedding"" <-> @embedding
            LIMIT @limit;";

                var semanticDto = await MemoryDTOs
                    .FromSqlRaw(sql,
                        new NpgsqlParameter("userId", userId),
                        new NpgsqlParameter("embedding", queryEmbedding),
                        new NpgsqlParameter("limit", semanticLimit))
                    .AsNoTracking()
                    .ToListAsync();

                semanticMemories = [.. semanticDto.Select(d => new Memory
                {
                    Id = d.Id,
                    UserId = d.UserId,
                    CreatedAt = d.CreatedAt,
                    UserMessage = d.UserMessage,
                    BotResponse = d.BotResponse,
                    Ephemeral = d.Ephemeral
                })];

                temporalMemories = [];
            }

            // merge and deduplicate with dictionary
            var merged = new Dictionary<int, Memory>();
            foreach (var m in semanticMemories)
                merged[m.Id] = m;
            foreach (var m in temporalMemories)
                merged.TryAdd(m.Id, m);

            return new HybridMemoryResult(
                [.. merged.Values],
                SemanticCount: semanticMemories.Count,
                TemporalCount: temporalMemories.Count);
        }

        public async Task<List<Memory>> GetRecentConversationAsync(
            string userId,
            int limit = 5,
            TimeSpan? maxGap = null)
        {
            maxGap ??= TimeSpan.FromMinutes(30);

            var ordered = await Memory
                .Where(m => m.UserId == userId && !m.Ephemeral)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit * 3)
                .Select(m => new Memory
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    CreatedAt = m.CreatedAt,
                    UserMessage = m.UserMessage,
                    BotResponse = m.BotResponse,
                    Ephemeral = m.Ephemeral
                })
                .AsNoTracking()
                .ToListAsync();

            if (ordered.Count == 0)
                return [];

            ordered.Reverse();

            var cluster = new List<Memory> { ordered.Last() };
            for (int i = ordered.Count - 2; i >= 0; i--)
            {
                var newer = cluster.First();
                var older = ordered[i];
                if (newer.CreatedAt - older.CreatedAt > maxGap)
                    break;
                cluster.Insert(0, older);
                if (cluster.Count >= limit)
                    break;
            }

            return cluster;
        }

        /// <summary>
        /// Retrieves a tag from the database by its ID.
        /// </summary>
        /// <param name="id">The unique ID of the tag (usually the server ID).</param>
        /// <returns>The corresponding <see cref="Tag"/> object if found; otherwise, null.</returns>
        public virtual async Task<Tag> GetTag(int id)
        {
            return await Tag.FirstOrDefaultAsync(t => t.Id == id);
        }

        /// <summary>
        /// Retrieves a tag from the database by its guild ID and name.
        /// </summary>
        /// <param name="guildId">The unique ID of the guild.</param>
        /// <param name="name">The name of the tag.</param>
        /// <returns>The corresponding <see cref="Tag"/> object if found; otherwise, null.</returns>
        public virtual async Task<Tag> GetTag(ulong guildId, string name)
        {
#pragma warning disable CA1862
            return await Tag.FirstOrDefaultAsync(t => t.GuildId == guildId &&
                t.Name == name.Trim().ToLowerInvariant());
#pragma warning restore CA1862
        }

        /// <summary>
        /// Removes a tag from the database.
        /// </summary>
        /// <param name="tag">The <see cref="Tag"/> entity to remove.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        public virtual async Task RemoveTag(Tag tag)
        {
            Tag.Remove(tag);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Removes all tags associated with a specific guild ID.
        /// </summary>
        /// <param name="guildId">The unique ID of the guild.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        public virtual async Task RemoveTags(List<Tag> tags)
        {
            Tag.RemoveRange(tags);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Adds a new tag to the database.
        /// </summary>
        /// <param name="tag">The <see cref="Tag"/> entity to add.</param>
        /// <returns>A task that represents the asynchronous insert operation.</returns>
        public virtual async Task AddTag(Tag tag)
        {
            Tag.Add(tag);
            await SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves all tags associated with a specific guild ID.
        /// </summary>
        /// <param name="guildId">The unique ID of the guild.</param>
        /// <returns>A list of <see cref="Tag"/> objects associated with the specified guild ID.</returns>
        public virtual async Task<List<Tag>> GetTagsByGuildId(ulong guildId)
        {
            return await Tag.Where(t => t.GuildId == guildId).ToListAsync();
        }
    }
}
