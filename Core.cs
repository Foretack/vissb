using System.Text.RegularExpressions;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace Core
{
    public class Core
    {
        public static DateTime StartupTime = new();
        public static DateTime DownTime = new();

        static void Main(string[] args)
        {
            Bot bot = new();
            StartupTime = DateTime.Now;
            AskCommand.Requests.DefaultRequestHeaders.Add("Authorization", Bot.OpenAIToken);
            Console.ReadLine();
        }

    }

    public class Bot
    {
        /*
         * IMPORTANT: Your token must have the required scopes to see when a channel goes live.
         * If your token does not have the required scopes, it will always think the channel is 
         * offline and continue to reply.
         * 
         * You can generate a token with the required scopes here:
         * https://twitchtokengenerator.com/
         * 
         * choose custom scope token on the pop up, then scroll down until you see the 
         * [Select All] button. Press the button, then generate your token (from the 
         * [Generate Token] button on the right side of the [Select All] button).
         */
        public const string Username = "TWITCH_USERNAME";
        public const string Token = "ACCESS_TOKEN";
        public const string OpenAIToken = "Bearer OPEN_AI_AUTH";
        public const string Channel = "minusinsanity";
        public const int ChannelID = 17497365;

        public static TwitchClient client = new();

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
                Disconnected = true;
                Core.DownTime = DateTime.Now;
                System.Timers.Timer timer = new();
                timer.Interval = 5000;
                timer.AutoReset = true;
                timer.Enabled = true;

                timer.Elapsed += (s, e) =>
                {
                    client.Connect();
                };
                client.OnConnected += async (s, e) =>
                {
                    if (Disconnected)
                    {
                        Console.WriteLine($"Reconnected after {(int)(DateTime.Now - Core.DownTime).TotalSeconds}s");
                        client.JoinChannel(Channel);
                        await PubSub.AttemptReconnect();
                        timer.Stop(); 
                        Disconnected = false;
                    }
                };
            };

            client.Connect();
        }

        private static bool Disconnected = false;

        public async Task HandleMessage(TwitchLib.Client.Events.OnMessageReceivedArgs Received)
        {
            string message = Received.ChatMessage.Message;
            string prompt = string.Empty;

            if (message.StartsWith("!ping"))
            {
                TimeSpan uptime = DateTime.Now - Core.StartupTime;
                client.SendMessage(Received.ChatMessage.Channel, $":) uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");
            }
            if (message.ToLower().StartsWith(Username + " "))
            {
                prompt = message.Replace(Username + " ", "");

                if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrEmpty(prompt)) return;

                await AskCommand.RunCommand(Received.ChatMessage.Username, prompt);
            }
            else if (message.ToLower().EndsWith(" " + Username))
            {
                prompt = message.Replace(" " + Username, "");

                if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrEmpty(prompt)) return;

                await AskCommand.RunCommand(Received.ChatMessage.Username, prompt);
            }
        }
    }
}