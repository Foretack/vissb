using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace Core
{
    public static class Core
    {
        public static DateTime StartupTime { get; private set; } = new();
        public static DateTime DownTime { get; set; } = new();

        static void Main(string[] args)
        {
            Bot bot = new();
            bot.Run();
            StartupTime = DateTime.Now;
            AskCommand.Requests.DefaultRequestHeaders.Add("Authorization", Config.OpenAIToken);
            Console.ReadLine();
        }

    }

    public class Bot
    {
        public static TwitchClient client { get; private set; } = new();

        public void Run()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(Config.Username, Config.Token);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, Config.Channel);

            client.OnJoinedChannel += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now}] Connected to {e.Channel}");
            };
            client.OnMessageReceived += async (s, e) =>
            {
                await HandleMessage(e.ChatMessage);
            };
            client.OnConnected += async (s, e) =>
           {
               if (!Disconnected)
               {
                   Console.ForegroundColor = ConsoleColor.Magenta;
                   Console.WriteLine($"[{DateTime.Now}] --- Connected");

                   PubSub pubSub = new();
                   pubSub.Run();
                   AskCommand.HandleMessageQueue();

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
                        Disconnected = false;
                    }
                };
            };

            client.Connect();
        }

        private bool Disconnected = false;

        public async Task HandleMessage(ChatMessage Received)
        {
            string prompt;

            if (Received.Message.StartsWith("!ping"))
            {
                TimeSpan uptime = DateTime.Now - Core.StartupTime;
                client.SendMessage(Received.Channel, $":) uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");
            }
            if (Received.Message.ToLower().StartsWith(Config.Username + " "))
            {
                prompt = Received.Message.Replace(Config.Username + " ", "");

                if (string.IsNullOrWhiteSpace(prompt)) return;

                await AskCommand.RunCommand(Received.Username, prompt);
            }
            else if (Received.Message.ToLower().EndsWith(" " + Config.Username))
            {
                prompt = Received.Message.Replace(" " + Config.Username, "");

                if (string.IsNullOrWhiteSpace(prompt)) return;

                await AskCommand.RunCommand(Received.Username, prompt);
            }
        }

        private async Task ReconnectShit()
        {
            Disconnected = false;
            client.JoinChannel(Config.Channel);
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