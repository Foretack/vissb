using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MiniTwitch.Irc;
using MiniTwitch.Irc.Models;
using Serilog;
using SystemTimer = System.Timers.Timer;

namespace vissb;
internal sealed class Bot
{
    private static readonly Regex _braille = new(@"[\u2580-\u259F]|[\u2801-\u2880]|⣿", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));
    private static readonly string[] _blacklistedUsers = { "titlechange_bot", "supibot", "streamelements", "megajumpbot", "pajbot" };
    private static (int Day, int Tokens) _dailyUsage = (DateTime.Now.Day, 0);
    private readonly PriorityQueue<string, byte> _messageQueue = new();
    private readonly IrcClient _client;
    private readonly DateTime _spawnTime;

    public Bot()
    {
        _client = new(options =>
        {
            options.Username = AppConfig.Username;
            options.OAuth = AppConfig.AccessToken;
            options.Logger = new LoggerFactory().AddSerilog(Log.Logger);
        });

        _client.OnConnect += async () =>
        {
            _ = await _client.JoinChannel(AppConfig.Channel);
        };
        _client.OnMessage += OnMessage;
        _spawnTime = DateTime.Now;

        SystemTimer timer = new()
        {
            Interval = TimeSpan.FromSeconds(3.5).TotalMilliseconds,
            AutoReset = true,
            Enabled = true
        };
        timer.Elapsed += (_, _) => Elapsed();
    }

    public Task<bool> Connect() => _client.ConnectAsync();

    private async void Elapsed()
    {
        if (_messageQueue.TryDequeue(out string? str, out _))
        {
            await _client.SendMessage(AppConfig.Channel, str);
        }
    }

    private async ValueTask OnMessage(Privmsg message)
    {
        if (StreamMonitor.StreamOnline || _blacklistedUsers.Contains(message.Author.Name))
            return;
        if (_braille.IsMatch(message.Content))
            return;
        if (DateTime.Now.Day != _dailyUsage.Day)
            _dailyUsage = (DateTime.Now.Day, 0);

        string[] args = message.Content.Split(' ');
        if (args.Length == 0)
            return;

        if (args[0] == AppConfig.PingCommand)
        {
            TimeSpan uptime = DateTime.Now - _spawnTime;
            _messageQueue.Enqueue($"{message.Author.Name}, hi :) {uptime:hh'h'mm'm'ss's'}, {_dailyUsage.Tokens} tokens used today", 15);
        }
        else if (args[0] == AppConfig.ForgetCommand)
        {
            OpenAiInteraction.ForgetContex(message.Author.Name);
            _messageQueue.Enqueue($"{message.Author.Name}, 🗑 ✅ ", 15);
            return;
        }

        if (args.Length < 2)
            return;

        if (AppConfig.DailyTokenUsageLimit > 0 && _dailyUsage.Tokens >= AppConfig.DailyTokenUsageLimit)
            return;

        if (args[0].ToLower().Contains(AppConfig.Username))
        {
            (string, byte, int) response = await OpenAiInteraction.Complete(
                    message.Author.Name,
                    message.Content);
            _messageQueue.Enqueue(response.Item1, response.Item2);
            _dailyUsage.Tokens += response.Item3;
            if (AppConfig.Notify && response.Item3 >= AppConfig.TokenThreshold)
                await Notify(response.Item3, message.Content);
            return;
        }
    }

    private static async Task Notify(int tokens, string prompt)
    {
        if (string.IsNullOrEmpty(AppConfig.NotifyWebhookLink))
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
        _ = await http.PostAsJsonAsync(AppConfig.NotifyWebhookLink, message);
    }
}
