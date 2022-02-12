using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace Core
{
    public class Core
    {
        static void Main(string[] args)
        {
            Bot bot = new();
            Console.ReadLine();
        }

    }

    public class Bot
    {
        public static readonly string Username = "TWITCH_USERNAME";
        public static readonly string Token = "ACCESS_TOKEN_GOES_HERE";

        TwitchClient client;

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
            client.Initialize(credentials, "minusinsanity");

            client.OnJoinedChannel += (s, e) =>
            {
                Console.WriteLine($"Connected to {e.Channel}");
            };
            client.OnMessageReceived += (s, e) =>
            {
                //
            };
            client.OnConnected +=  (s, e) =>
            {
                Console.WriteLine("connected");
                PubSub pubSub = new();
            };

            client.Connect();
        }
    }
}