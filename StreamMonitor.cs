using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Services;

namespace Core
{
    public static class StreamMonitor
    {
        public static bool StreamOnline { get; private set; } = false;

        private static readonly TwitchAPI TwitchAPI = new TwitchAPI(settings: new ApiSettings() { AccessToken = Config.Token, ClientId = Config.ClientID });
        private static readonly LiveStreamMonitorService Monitor = new LiveStreamMonitorService(TwitchAPI, 10);

        public static void Initialize()
        {
            Monitor.SetChannelsByName(new List<string> { Config.Channel });
            Monitor.Start();

            Monitor.OnStreamOnline += (s, e) => SetStreamOnline();
            Monitor.OnStreamOffline += (s, e) => SetStreamOffline();
        }

        private static void SetStreamOnline() { StreamOnline = true; }
        private static void SetStreamOffline() { StreamOnline = false; }
    }
}
