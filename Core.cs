using System.Text.RegularExpressions;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

#pragma warning disable CS8618

namespace Core
{
    public class Core
    {
        public static readonly DateTime StartupTime = DateTime.Now;
        public static DateTime DownTime = new();

        static void Main(string[] args)
        {
            Bot bot = new();
            AskCommand.Requests.DefaultRequestHeaders.Add("Authorization", Bot.OpenAIToken);
            Console.ReadLine();
        }

    }

    public class Bot
    {
        public const string Username = "TWITCH_USERNAME";
        public const string Token = "ACCESS_TOKEN";
        public const string OpenAIToken = "Bearer OPEN_AI_AUTH";
        public const string Channel = "minusinsanity";
        public const int ChannelID = 17497365;

        public static TwitchClient client;

        public Bot()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(Username, Token);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, Channel);

            client.OnJoinedChannel += (s, e) =>
            {
                Console.WriteLine($"Connected to {e.Channel}");
            };
            client.OnMessageReceived += async (s, e) =>
            {
                await HandleMessage(e);
            };
            client.OnConnected +=  (s, e) =>
            {
                Console.WriteLine("connected");
                client.JoinChannel("foretack");
                PubSub pubSub = new();
                AskCommand ask = new();
            };
            client.OnDisconnected += (s, e) =>
            {
                Core.DownTime = DateTime.Now;
                System.Timers.Timer timer = new();
                timer.Interval = 3000;
                timer.AutoReset = true;
                timer.Enabled = true;

                timer.Elapsed += (s, e) =>
                {
                    client.Connect();
                };
                client.OnConnected += (s, e) =>
                {
                    Console.WriteLine($"Reconnected after {(DateTime.Now - Core.DownTime).Seconds}s");
                    timer.Stop();
                };
            };

            client.Connect();
        }

        public async Task HandleMessage(TwitchLib.Client.Events.OnMessageReceivedArgs Received)
        {
            string message = Received.ChatMessage.Message;
            string prompt = string.Empty;

            if (Received.ChatMessage.Username == "streamelements" && message.StartsWith("NaM"))
            {
                client.SendMessage("foretack", $"NaM 154834 .vissb uptime: {(DateTime.Now - Core.StartupTime)}");
            }
            if (message.ToLower().StartsWith(Bot.Username + " "))
            {
                prompt = message.Replace(Bot.Username + " ", "");

                if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrEmpty(prompt)) return;

                await AskCommand.RunCommand(Received.ChatMessage.Username, prompt);
            }
            else if (message.ToLower().EndsWith(" " + Bot.Username))
            {
                prompt = message.Replace(" " + Bot.Username, "");

                if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrEmpty(prompt)) return;

                await AskCommand.RunCommand(Received.ChatMessage.Username, prompt);
            }
        }
    }
}