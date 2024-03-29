﻿using Config.Net;

namespace vissb;

internal static class ConfigLoader
{
    public static IAppConfig AppConfig { get; } = new ConfigurationBuilder<IAppConfig>()
        .UseYamlFile("config.yml")
        .Build();
}
