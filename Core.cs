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
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now}] Connected to {e.Channel}");
            };
            client.OnMessageReceived += async (s, e) =>
            {
                await HandleMessage(e.ChatMessage);
            };
            client.OnConnected +=  async (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[{DateTime.Now}] --- Connected");

                PubSub pubSub = new();
                AskCommand ask = new();

                short updates = await PubSub.CheckStreamStatus();
                Console.WriteLine($"{updates} viewcount updates");

                if (updates == 0)
                {
                    Console.WriteLine($"[{DateTime.Now}] The stream is currently offline, resuming replies. ");
                    AskCommand.StreamOnline = false;
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] The stream is currently online, replies disabled. ");
                    AskCommand.StreamOnline = true;
                }
            };
            client.OnDisconnected += (s, e) =>
            {
                Disconnected = true;
                Core.DownTime = DateTime.Now;

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[{DateTime.Now}] --- Disconnected");

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
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Reconnected after {(int)(DateTime.Now - Core.DownTime).TotalSeconds}s");

                        await ReconnectShit();
                        timer.Stop(); 
                    }
                };
            };

            client.Connect();
        }


        public async Task HandleMessage(ChatMessage Received)
        {
            string prompt;

            if (Received.Message.StartsWith("!ping"))
            {
                TimeSpan uptime = DateTime.Now - Core.StartupTime;
                client.SendMessage(Received.Channel, $":) uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");
            }
            if (Received.Message.ToLower().StartsWith(Username + " "))
            {
                prompt = Received.Message.Replace(Username + " ", "");

                if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrEmpty(prompt)) return;

                await AskCommand.RunCommand(Received.Username, prompt);
            }
            else if (Received.Message.ToLower().EndsWith(" " + Username))
            {
                prompt = Received.Message.Replace(" " + Username, "");

                if (string.IsNullOrWhiteSpace(prompt) || string.IsNullOrEmpty(prompt)) return;

                await AskCommand.RunCommand(Received.Username, prompt);
            }
        }

        private static bool Disconnected = false;
        private protected static async Task ReconnectShit()
        {
            Disconnected = false;
            client.JoinChannel(Channel);
            PubSub.AttemptReconnect().Wait();
            short updates = await PubSub.CheckStreamStatus();
            Console.WriteLine($"{updates} viewcount updates");

            if (updates == 0)
            {
                Console.WriteLine($"[{DateTime.Now}] The stream is currently offline, resuming replies. ");
                AskCommand.StreamOnline = false;
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now}] The stream is currently online, replies disabled. ");
                AskCommand.StreamOnline = true;
            }
        }
    }
}