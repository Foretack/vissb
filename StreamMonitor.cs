﻿using Serilog;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Services;

namespace vissb;

public static class StreamMonitor
{
    public static bool StreamOnline { get; private set; } = false;

    private static readonly TwitchAPI TwitchAPI = new(settings: new ApiSettings()
    {
        AccessToken = ConfigLoader.AppConfig.AccessToken,
        ClientId = ConfigLoader.AppConfig.ClientId
    });
    private static readonly LiveStreamMonitorService Monitor = new(TwitchAPI, 30);

    public static void Initialize()
    {
        Monitor.SetChannelsByName(new List<string> { ConfigLoader.AppConfig.Channel });

        Monitor.OnStreamOnline += (_, _) => { StreamOnline = true; Log.Debug("{channel} has gone live", ConfigLoader.AppConfig.Channel); };
        Monitor.OnStreamOffline += (_, _) => { StreamOnline = false; Log.Debug("{channel} has gone offline", ConfigLoader.AppConfig.Channel); };
        Monitor.OnServiceStarted += (_, _) => Log.Information("StreamMonitor service started!");
        Monitor.OnServiceStopped += (_, _) => Log.Warning("StreamMonitor service stopped!");

        Monitor.Start();
    }
}
