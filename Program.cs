global using static vissb.ConfigLoader;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace vissb;
public static class Program
{
    public static DateTime StartupTime { get; private set; }

    private static readonly LoggingLevelSwitch LogSwitch = new();
    public static async Task Main()
    {
        LogSwitch.MinimumLevel = LogEventLevel.Information;
        Log.Logger = new LoggerConfiguration().MinimumLevel.ControlledBy(LogSwitch).WriteTo.Console().CreateLogger();
        StartupTime = DateTime.Now;

        Bot bot = new();
        _ = await bot.Connect();

        while (true)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrEmpty(input))
                continue;

            if (Enum.TryParse(input, out LogEventLevel level))
            {
                LogSwitch.MinimumLevel = level;
                Console.WriteLine($"Switching logging level to: {level}");
            }
        }
    }
}
