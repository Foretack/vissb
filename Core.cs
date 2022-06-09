using Serilog;
using Serilog.Core;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Events;
using TwitchLib.Client.Events;

namespace Core;
public static class Core
{
    public static DateTime StartupTime { get; private set; }

    private static LoggingLevelSwitch LogSwitch = new LoggingLevelSwitch();
    public static async Task Main(string[] args)
    {
        LogSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
        Log.Logger = new LoggerConfiguration().MinimumLevel.ControlledBy(LogSwitch).WriteTo.Console().CreateLogger();

        await Bot.Initialize();
        Console.ReadLine();
    }
}

public static class Bot
{
    public static TwitchClient Client { get; private set; } = new TwitchClient();

    public static Task Initialize()
    {
        ConnectionCredentials credentials = new ConnectionCredentials(Config.Username, Config.Token);
        ClientOptions options = new ClientOptions()
        {
            MessagesAllowedInPeriod = 100,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };
        WebSocketClient wsClient = new WebSocketClient(options);
        Client = new TwitchClient(wsClient);
        Client.AutoReListenOnException = true;
        Client.Initialize(credentials, Config.Channel);

        Client.OnIncorrectLogin += (s, e) => { Log.Fatal(e.Exception, $"The account creditentials you provided are invalid!"); throw e.Exception; };
        Client.OnConnected += (s, e) => { Log.Information($"Connected as {e.BotUsername}"); };
        Client.OnJoinedChannel += (s, e) => { Log.Information($"Joined {e.Channel}"); };
        Client.OnConnectionError += OnConnectionError;
        Client.OnReconnected += OnReconnected;
        Client.OnMessageReceived += OnMessageReceived;

        StreamMonitor.Initialize();
        Client.Connect();
        AskCommand.Requests.DefaultRequestHeaders.Add("Authorization", Config.OpenAIToken);
        AskCommand.HandleMessageQueue();

        return Task.CompletedTask;
    }

    private static void OnReconnected(object? sender, OnReconnectedEventArgs e)
    {
        Log.Information("Reconnected. Attempting to rejoin channel...");
        Client.JoinChannel(Config.Channel);
    }

    private static void OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        Log.Warning($"A connection error has occured. Attempting to reconnect...");
    }

    private static async void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        await HandleMessage(e.ChatMessage);
    }

    private static async ValueTask HandleMessage(ChatMessage ircMessage)
    {
        string[] args = ircMessage.Message.Split(' ');

        if (args.Length == 1) return;

        if (args[0].Contains(Config.Username))
        {
            await AskCommand.Run(ircMessage.Username, string.Join(' ', args[1..]));
        }
        if (args[^1].Contains(Config.Username))
        {
            await AskCommand.Run(ircMessage.Username, string.Join(' ', args[..^1]));
        }
    }
}
