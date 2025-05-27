using Bob.Database.Types;

namespace Bob.Commands.Helpers
{
    public static class ReactBoardMethods
    {
        public static bool isSetup(Server server) {
            return server.ReactBoardOn && server.ReactBoardChannelId.HasValue && server.ReactBoardEmoji != null && server.ReactBoardEmoji.Length > 0;
        }
    }
}