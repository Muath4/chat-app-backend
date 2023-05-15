using System.Collections.Concurrent;

namespace ChatApp.Server.Hubs
{
    public static class GroupHandler
    {
        public static ConcurrentDictionary<string, HashSet<string>> GroupConnections = new ConcurrentDictionary<string, HashSet<string>>();
    }
}