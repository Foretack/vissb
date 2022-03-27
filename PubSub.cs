using System;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace Core
{
    public class PubSub
    {
        public static TwitchPubSub Client = new();

        private static short Status = 0;

        public PubSub()
        {
            Run();
        }
        public static async Task AttemptReconnect()
        {
            Client.Disconnect();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[{DateTime.Now}] Reconnecting PubSub...");
            await Task.Delay(5000);
            Client.Connect();
        }

        public static async Task<short> CheckStreamStatus()
        {
            Status = 0;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[{DateTime.Now}] Checking if the stream is currently on...");
            await Task.Delay(30000);

            return Status;
        }

        private void Run()
        {
            Client = new TwitchPubSub();

            Client.OnPubSubServiceConnected += (s, e) =>
            {
                Client.SendTopics();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now}] PubSub connected");
            };
            Client.OnListenResponse += (s, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                if (!e.Successful) throw new Exception($"Failed to listen! Response: {e.Response}");
                else Console.WriteLine($"listening to {e.Topic}");
            };
            Client.OnStreamDown += (s, e) =>
            {
                AskCommand.StreamOnline = false;
                Status = 0;

                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"[{DateTime.Now}] --- Stream offline, replies enabled ---");
            };
            Client.OnStreamUp += (s, e) =>
            {
                AskCommand.StreamOnline = true;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now}] --- Stream online, replies disabled ---");
            };
            Client.OnViewCount += (s, e) =>
            {
                Status += 1;
            };

            Client.ListenToVideoPlayback(Bot.ChannelID.ToString());

            Client.Connect();
        }
    }
}
