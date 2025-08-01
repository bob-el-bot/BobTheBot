using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bob.Commands.Helpers;
using Bob.Database;
using Bob.Database.Types;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

public class DualWriteBobEntities : BobEntities
{
    private readonly BobEntities _oldDb;
    private readonly BobEntities _newDb;

    public DualWriteBobEntities(string oldConnStr, string newConnStr)
    {
        _oldDb = new BobEntities(oldConnStr);
        _newDb = new BobEntities(newConnStr);
    }

    // Expose your DbSets for reads (from old DB)
    public override DbSet<Server> Server => _oldDb.Server;
    public override DbSet<WelcomeImage> WelcomeImage => _oldDb.WelcomeImage;
    public override DbSet<User> User => _oldDb.User;
    public override DbSet<NewsChannel> NewsChannel => _oldDb.NewsChannel;
    public override DbSet<BlackListUser> BlackListUser => _oldDb.BlackListUser;
    public override DbSet<ScheduledMessage> ScheduledMessage => _oldDb.ScheduledMessage;
    public override DbSet<ScheduledAnnouncement> ScheduledAnnouncement => _oldDb.ScheduledAnnouncement;
    public override DbSet<ReactBoardMessage> ReactBoardMessage => _oldDb.ReactBoardMessage;

    public static string BuildConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        return new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = userInfo[0],
            Password = userInfo[1],
            Database = uri.AbsolutePath.TrimStart('/')
        }.ToString();
    }

    public override int SaveChanges()
    {
        var result = _oldDb.SaveChanges();
        _newDb.SaveChanges();
        return result;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _oldDb.SaveChangesAsync(cancellationToken);
        await _newDb.SaveChangesAsync(cancellationToken);
        return result;
    }

    public override void Dispose()
    {
        _oldDb.Dispose();
        _newDb.Dispose();
        base.Dispose();
    }

    // Read-only methods
    public override Task<double> GetDatabaseSizeBytes() => _oldDb.GetDatabaseSizeBytes();
    public override Task<ulong> GetTotalEntries() => _oldDb.GetTotalEntries();
    public override Task<NewsChannel> GetNewsChannel(ulong id) => _oldDb.GetNewsChannel(id);
    public override Task<BlackListUser> GetUserFromBlackList(ulong id) => _oldDb.GetUserFromBlackList(id);
    public override Task<ScheduledMessage> GetScheduledMessage(ulong id) => _oldDb.GetScheduledMessage(id);
    public override Task<ScheduledAnnouncement> GetScheduledAnnouncement(ulong id) => _oldDb.GetScheduledAnnouncement(id);
    public override Task<WelcomeImage> GetWelcomeImage(ulong id) => _oldDb.GetWelcomeImage(id);
    public override Task<ReactBoardMessage> GetReactBoardMessageAsync(ulong originalMessageId) => _oldDb.GetReactBoardMessageAsync(originalMessageId);
    public override Task<List<ReactBoardMessage>> GetAllReactBoardMessagesForGuildAsync(ulong guildId) => _oldDb.GetAllReactBoardMessagesForGuildAsync(guildId);

    // Dual-write methods (write to both DBs)
    public override async Task<Server> GetServer(ulong id)
    {
        var server = await _oldDb.Server.FindAsync(keyValues: id);
        var serverNew = await _newDb.Server.FindAsync(keyValues: id);

        if (server == null && serverNew == null)
        {
            var newServer = new Server { Id = id };
            await _oldDb.Server.AddAsync(newServer);
            await _oldDb.SaveChangesAsync();
            await _newDb.Server.AddAsync(newServer);
            await _newDb.SaveChangesAsync();
            return newServer;
        }
        else if (server == null)
        {
            // Copy from new to old if only new exists
            await _oldDb.Server.AddAsync(serverNew);
            await _oldDb.SaveChangesAsync();
            return serverNew;
        }
        else if (serverNew == null)
        {
            // Copy from old to new if only old exists
            await _newDb.Server.AddAsync(server);
            await _newDb.SaveChangesAsync();
            return server;
        }
        else
        {
            return server;
        }
    }

    public override async Task RemoveServer(Server server)
    {
        _oldDb.Server.Remove(server);
        _newDb.Server.Remove(server);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task<User> GetUser(ulong id)
    {
        var user = await _oldDb.User.FindAsync(keyValues: id);
        var userNew = await _newDb.User.FindAsync(keyValues: id);

        if (user == null && userNew == null)
        {
            var newUser = new User { Id = id };
            await _oldDb.User.AddAsync(newUser);
            await _oldDb.SaveChangesAsync();
            await _newDb.User.AddAsync(newUser);
            await _newDb.SaveChangesAsync();
            return newUser;
        }
        else if (user == null)
        {
            await _oldDb.User.AddAsync(userNew);
            await _oldDb.SaveChangesAsync();
            return userNew;
        }
        else if (userNew == null)
        {
            await _newDb.User.AddAsync(user);
            await _newDb.SaveChangesAsync();
            return user;
        }
        else
        {
            return user;
        }
    }

    public override async Task UpdateUser(User user)
    {
        _oldDb.User.Update(user);
        _newDb.User.Update(user);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task<List<User>> GetUsers(IEnumerable<ulong> ids)
    {
        var users = await _oldDb.User.Where(u => ids.Contains(u.Id)).ToListAsync();
        var usersNew = await _newDb.User.Where(u => ids.Contains(u.Id)).ToListAsync();
        var missingIds = ids.Except(users.Select(u => u.Id));

        foreach (var id in missingIds)
        {
            var newUser = new User { Id = id };
            await _oldDb.User.AddAsync(newUser);
            await _newDb.User.AddAsync(newUser);
            users.Add(newUser);
        }

        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();

        return users;
    }

    public override async Task UpdateUsers(IEnumerable<User> users)
    {
        foreach (var user in users)
        {
            _oldDb.User.Update(user);
            _newDb.User.Update(user);
        }
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task UpdateNewsChannel(NewsChannel newsChannel)
    {
        _oldDb.NewsChannel.Update(newsChannel);
        _newDb.NewsChannel.Update(newsChannel);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task RemoveNewsChannel(NewsChannel newsChannel)
    {
        _oldDb.NewsChannel.Remove(newsChannel);
        _newDb.NewsChannel.Remove(newsChannel);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task AddNewsChannel(NewsChannel newsChannel)
    {
        await _oldDb.NewsChannel.AddAsync(newsChannel);
        await _newDb.NewsChannel.AddAsync(newsChannel);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task UpdateUserFromBlackList(BlackListUser user)
    {
        _oldDb.BlackListUser.Update(user);
        _newDb.BlackListUser.Update(user);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task RemoveUserFromBlackList(BlackListUser user)
    {
        _oldDb.BlackListUser.Remove(user);
        _newDb.BlackListUser.Remove(user);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task AddUserToBlackList(BlackListUser user)
    {
        await _oldDb.BlackListUser.AddAsync(user);
        await _newDb.BlackListUser.AddAsync(user);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task UpdateScheduledMessage(ScheduledMessage message)
    {
        _oldDb.ScheduledMessage.Update(message);
        _newDb.ScheduledMessage.Update(message);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task RemoveScheduledMessage(ulong messageId)
    {
        await _oldDb.Database.ExecuteSqlRawAsync("DELETE FROM \"ScheduledMessage\" WHERE \"Id\" = @p0", messageId);
        await _newDb.Database.ExecuteSqlRawAsync("DELETE FROM \"ScheduledMessage\" WHERE \"Id\" = @p0", messageId);
    }

    public override async Task AddScheduledMessage(ScheduledMessage message)
    {
        await _oldDb.ScheduledMessage.AddAsync(message);
        await _newDb.ScheduledMessage.AddAsync(message);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task UpdateScheduledAnnouncement(ScheduledAnnouncement announcement)
    {
        _oldDb.ScheduledAnnouncement.Update(announcement);
        _newDb.ScheduledAnnouncement.Update(announcement);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task RemoveScheduledAnnouncement(ulong announcementId)
    {
        await _oldDb.Database.ExecuteSqlRawAsync("DELETE FROM \"ScheduledAnnouncement\" WHERE \"Id\" = @p0", announcementId);
        await _newDb.Database.ExecuteSqlRawAsync("DELETE FROM \"ScheduledAnnouncement\" WHERE \"Id\" = @p0", announcementId);
    }

    public override async Task AddScheduledAnnouncement(ScheduledAnnouncement announcement)
    {
        await _oldDb.ScheduledAnnouncement.AddAsync(announcement);
        await _newDb.ScheduledAnnouncement.AddAsync(announcement);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task RemoveScheduledItem(IScheduledItem item)
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

    public override async Task UpdateWelcomeImage(WelcomeImage image)
    {
        _oldDb.WelcomeImage.Update(image);
        _newDb.WelcomeImage.Update(image);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task RemoveWelcomeImage(WelcomeImage image)
    {
        _oldDb.WelcomeImage.Remove(image);
        _newDb.WelcomeImage.Remove(image);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task AddWelcomeImage(WelcomeImage image)
    {
        await _oldDb.WelcomeImage.AddAsync(image);
        await _newDb.WelcomeImage.AddAsync(image);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task AddReactBoardMessageAsync(ReactBoardMessage message)
    {
        await _oldDb.ReactBoardMessage.AddAsync(message);
        await _newDb.ReactBoardMessage.AddAsync(message);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task RemoveReactBoardMessageAsync(ReactBoardMessage message)
    {
        _oldDb.ReactBoardMessage.Remove(message);
        _newDb.ReactBoardMessage.Remove(message);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task AddInitialReactBoardMessageAsync(ulong guildId)
    {
        bool anyExistOld = await _oldDb.ReactBoardMessage.AnyAsync(x => x.GuildId == guildId);
        bool anyExistNew = await _newDb.ReactBoardMessage.AnyAsync(x => x.GuildId == guildId);

        if (anyExistOld && anyExistNew)
            return;

        var dummyMessage = new ReactBoardMessage
        {
            OriginalMessageId = guildId,
            GuildId = guildId
        };

        if (!anyExistOld)
            await _oldDb.ReactBoardMessage.AddAsync(dummyMessage);
        if (!anyExistNew)
            await _newDb.ReactBoardMessage.AddAsync(dummyMessage);

        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task AddMultipleReactBoardMessagesAsync(List<ReactBoardMessage> messages)
    {
        await _oldDb.ReactBoardMessage.AddRangeAsync(messages);
        await _newDb.ReactBoardMessage.AddRangeAsync(messages);
        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }

    public override async Task RemoveAllReactBoardMessagesForGuildAsync(ulong guildId)
    {
        var messagesOld = await _oldDb.ReactBoardMessage.Where(x => x.GuildId == guildId).ToListAsync();
        var messagesNew = await _newDb.ReactBoardMessage.Where(x => x.GuildId == guildId).ToListAsync();

        _oldDb.ReactBoardMessage.RemoveRange(messagesOld);
        _newDb.ReactBoardMessage.RemoveRange(messagesNew);

        await _oldDb.SaveChangesAsync();
        await _newDb.SaveChangesAsync();
    }
}