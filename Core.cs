using Serilog;
using Serilog.Core;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Events;
using TwitchLib.Client.Events;
using TwitchLib.Client.Exceptions;
using System.Diagnostics;
using System.Reflection;
using CliWrap;
using CliWrap.Buffered;

namespace Core;
public static class Core
{
    public static DateTime StartupTime { get; private set; }
    public static string AssemblyName { get; } = Assembly.GetEntryAssembly()?.GetName().Name ?? throw new ArgumentException($"{nameof(AssemblyName)} can not be null.");

    private static LoggingLevelSwitch LogSwitch = new LoggingLevelSwitch();
    public static async Task Main(string[] args)
    {
        LogSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Information;
        Log.Logger = new LoggerConfiguration().MinimumLevel.ControlledBy(LogSwitch).WriteTo.Console().CreateLogger();
        StartupTime = DateTime.Now;

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
        ReconnectionPolicy policy = new ReconnectionPolicy();
        policy.SetMaxAttempts(15);
        options.ReconnectionPolicy = policy;
        WebSocketClient wsClient = new WebSocketClient(options);
        Client = new TwitchClient(wsClient);
        Client.AutoReListenOnException = true;
        Client.Initialize(credentials, Config.Channel);

        Client.OnIncorrectLogin += (s, e) => { Log.Fatal(e.Exception, $"The account creditentials you provided are invalid!"); throw e.Exception; };
        Client.OnConnected += (s, e) => { Log.Information($"Connected as {e.BotUsername}"); };
        Client.OnJoinedChannel += (s, e) => { Log.Information($"Joined {e.Channel}"); };
        Client.OnMessageReceived += OnMessageReceived;
        
        Client.OnDisconnected += OnDisconnected;
        Client.OnError += OnError;
        Client.OnConnectionError += OnConnectionError;

        StreamMonitor.Initialize();
        Client.Connect();
        AskCommand.Requests.DefaultRequestHeaders.Add("Authorization", Config.OpenAIToken);
        AskCommand.HandleMessageQueue();

        return Task.CompletedTask;
    }

    private static void OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        Log.Fatal(e.Error.Message);
        RestartProcess();
    }

    private static void OnError(object? sender, OnErrorEventArgs e)
    {
        Log.Fatal(e.Exception.Message);
        RestartProcess();
        Console.Write(" ");
    }

    private static void OnDisconnected(object? sender, OnDisconnectedEventArgs e)
    {
        Log.Fatal("Disconnected");
        RestartProcess();
    }

    private static async void OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        try { await HandleMessage(e.ChatMessage); }
        catch (BadStateException) { Client.JoinChannel(Config.Channel); }
    }

    private static async ValueTask HandleMessage(ChatMessage ircMessage)
    {
        string[] args = ircMessage.Message.Split(' ');

        if (ircMessage.Message.StartsWith($"!{Config.Username} update")
        && ircMessage.Username == Config.HosterName)
        {
            var pullResults = await Cli.Wrap("git").WithArguments("pull").ExecuteBufferedAsync();
            string result = pullResults.StandardOutput
                .Split('\n')
                .First(x => x.Contains("files changed") || x.Contains("file changed") || x.Contains("Already up to date"));

            Client.SendMessage(Config.Channel, $"{result}");
            return;
        }
        if (ircMessage.Message.StartsWith("!ping"))
        {
            TimeSpan uptime = DateTime.Now - Core.StartupTime;
            string uptimeString = uptime.TotalDays >= 1 ? $"{uptime:d' days and 'h' hours'}" : $"{uptime:h'h'm'm's's'}";
            Client.SendMessage(Config.Channel, $"Pong! :) {uptimeString}");
        }

        if (args.Length == 1) return;

        if (args[0].ToLower().Contains(Config.Username))
        {
            await AskCommand.Run(ircMessage.Username, string.Join(' ', args[1..]));
            return;
        }
        if (args[^1].ToLower().Contains(Config.Username))
        {
            await AskCommand.Run(ircMessage.Username, string.Join(' ', args[..^1]));
            
        }
    }

    private static void RestartProcess()
    {
        Log.Fatal("Process is restarting...");
        Process.Start($"./{Core.AssemblyName}", Environment.GetCommandLineArgs());
        Environment.Exit(0);
    }
}
