using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Serilog;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using SystemTimer = System.Timers.Timer;

namespace vissb;
internal sealed class Bot
{
    private static readonly string[] _blacklistedUsers = { "titlechange_bot", "supibot", "streamelements", "megajumpbot", "pajbot" };
    private static (int Day, int Tokens) _dailyUsage = (DateTime.Now.Day, 0);
    private readonly PriorityQueue<string, byte> _messageQueue = new();
    private readonly TwitchClient _client;
    private readonly DateTime _spawnTime;

    public Bot()
    {
        var creditentials = new ConnectionCredentials(
            ConfigLoader.Config.Username,
            ConfigLoader.Config.AccessToken);


        WebSocketClient wsClient = new();
        _client = new TwitchClient(wsClient, logger: new LoggerFactory().AddSerilog(Log.Logger).CreateLogger<TwitchClient>());

        try
        {
            _client.Initialize(creditentials, ConfigLoader.Config.Channel);
        }
        catch (Exception)
        {
            throw;
        }

        _client.OnConnected += (s, e) => Log.Information("Connected as {username}.", e.BotUsername);
        _client.OnJoinedChannel += (s, e) => Log.Information("Joined {channel}.", e.Channel);
        _client.OnMessageReceived += async (s, e) => await OnMessage(e.ChatMessage);

        _client.OnIncorrectLogin += (_, _) => throw new Exception("The account creditentials you provided are invalid.");
        _client.OnDisconnected += (_, _) => throw new Exception("Disconnected.");
        _client.OnError += (s, e) => throw e.Exception;
        _client.OnConnectionError += (s, e) => throw new Exception(e.Error.Message);

        _ = _client.Connect();
        _spawnTime = DateTime.Now;

        SystemTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(3.5).TotalMilliseconds,
            AutoReset = true,
            Enabled = true
        };
        timer.Elapsed += (_, _) => Elapsed();
    }

    private void Elapsed()
    {
        if (_messageQueue.TryDequeue(out string? str, out _))
        {
            _client.SendMessage(ConfigLoader.Config.Channel, str);
        }
    }

    private async ValueTask OnMessage(ChatMessage ircMessage)
    {
        if (StreamMonitor.StreamOnline || _blacklistedUsers.Contains(ircMessage.Username))
            return;
        if (DateTime.Now.Day != _dailyUsage.Day)
            _dailyUsage = (DateTime.Now.Day, 0);

        string[] args = ircMessage.Message.Split(' ');
        if (args.Length == 0)
            return;
        if (args[0] == ConfigLoader.Config.PingCommand)
        {
            TimeSpan uptime = DateTime.Now - _spawnTime;
            _messageQueue.Enqueue($"{ircMessage.Username}, hi :) {uptime:hh'h'mm'm'ss's'}, {_dailyUsage.Tokens} tokens used today", 15);
            return;
        }
        if (args[0] == ConfigLoader.Config.ForgetCommand)
        {
            OpenAiInteraction.ForgetContex();
            _messageQueue.Enqueue($"{ircMessage.Username}, 🗑 ✅ ", 15);
            return;
        }

        if (args.Length < 2)
            return;
        if (ConfigLoader.Config.DailyTokenUsageLimit > 0
        && _dailyUsage.Tokens >= ConfigLoader.Config.DailyTokenUsageLimit)
        {
            return;
        }

        try
        {
            if (args.First().ToLower().Contains(ConfigLoader.Config.Username))
            {
                (string, byte, int) response = await OpenAiInteraction.Complete(
                    ircMessage.Username,
                    ircMessage.Message[args[0].Length..]);
                _messageQueue.Enqueue(response.Item1, response.Item2);
                _dailyUsage.Tokens += response.Item3;
                if (ConfigLoader.Config.Notify && response.Item3 >= ConfigLoader.Config.TokenThreshold)
                    await Notify(response.Item3, ircMessage.Message);
                return;
            }
            if (args.Last().ToLower().Contains(ConfigLoader.Config.Username))
            {
                (string, byte, int) response = await OpenAiInteraction.Complete(
                    ircMessage.Username,
                    ircMessage.Message[..(ircMessage.Message.Length - args[^0].Length)]);
                _messageQueue.Enqueue(response.Item1, response.Item2);
                _dailyUsage.Tokens += response.Item3;
                if (ConfigLoader.Config.Notify && response.Item3 >= ConfigLoader.Config.TokenThreshold)
                    await Notify(response.Item3, ircMessage.Message);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception caught: ");
        }
    }

    private static async Task Notify(int tokens, string prompt)
    {
        if (string.IsNullOrEmpty(ConfigLoader.Config.NotifyWebhookLink))
            return;
        var http = new HttpClient();
        var message = new
        {
            embeds = new[]
            {
                new
                {
                    title = "⚠ Request exceeded token threshold",
                    color = 16312092,
                    fields = new[]
                    {
                        new
                        {
                            name = "Prompt:",
                            value = prompt
                        },
                        new
                        {
                            name = "Usage:",
                            value = $"{tokens} tokens"
                        }
                    }
                }
            }
        };
        _ = await http.PostAsJsonAsync(ConfigLoader.Config.NotifyWebhookLink, message);
    }
}
