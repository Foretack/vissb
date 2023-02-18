using Serilog;
using Serilog.Core;

namespace vissb;
public static class Program
{
    public static DateTime StartupTime { get; private set; }

    private static readonly LoggingLevelSwitch LogSwitch = new();
    public static async Task Main()
    {
        LogSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
        Log.Logger = new LoggerConfiguration().MinimumLevel.ControlledBy(LogSwitch).WriteTo.Console().CreateLogger();
        StartupTime = DateTime.Now;

        await Start();

        _ = Console.ReadLine();
    }

    private static async Task Start()
    {
        try
        {
            Bot bot = new();
            StreamMonitor.Initialize();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Restarting bot");
            await Task.Delay(TimeSpan.FromSeconds(30));
            await Start();
        }
    }
}
