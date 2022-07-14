using Serilog;
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

            Monitor.OnStreamOnline += (_,_) => { StreamOnline = true; Log.Information($"{Config.Channel} is live!"); };
            Monitor.OnStreamOffline += (_,_) => { StreamOnline = false; Log.Information($"{Config.Channel} is offline!"); };
            Monitor.OnServiceStarted += (_,_) => Log.Information("StreamMonitor service started!");
            Monitor.OnServiceStopped += (_,_) => Log.Warning("StreamMonitor service stopped!");
            
            Monitor.Start();
        }
    }
}
