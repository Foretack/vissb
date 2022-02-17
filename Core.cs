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
        static void Main(string[] args)
        {
            Bot bot = new();
            AskCommand.Requests.DefaultRequestHeaders.Add("Authorization", Bot.OpenAIToken);
            Console.ReadLine();
        }

    }

    public class Bot
    {
        public static readonly string Username = "TWITCH_USERNAME";
        public static readonly string Token = "ACCESS_TOKEN_GOES_HERE";
        public static readonly string OpenAIToken = "Bearer OPEN_AI_AUTH";
        public static readonly string Channel = "minusinsanity";
        public static readonly int ChannelID = 17497365;

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
                PubSub pubSub = new();
                AskCommand.HandleMessageQueue();
            };

            client.Connect();
        }

        public async Task HandleMessage(TwitchLib.Client.Events.OnMessageReceivedArgs Received)
        {
            string message = Received.ChatMessage.Message;
            string prompt = string.Empty;

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