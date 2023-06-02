using System.Threading.Tasks;
using DiscordBotsList.Api;

public class TopGG
{
    AuthDiscordBotListApi DblApi = new AuthDiscordBotListApi(705680059809398804, Config.GetTopGGToken());

    public async Task PostStats()
    {
        ISelfBot bob = await DblApi.GetMeAsync();
        // Update stats           guildCount
        await bob.UpdateStatsAsync(Bot.Client.Guilds.Count);
    }
}