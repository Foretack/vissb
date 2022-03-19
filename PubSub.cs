using System;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace Core
{
    public class PubSub
    {
        public static TwitchPubSub client = new();

        public PubSub()
        {
            Run();
        }
        public static async Task AttemptReconnect()
        {
            client.Disconnect();
            await Task.Delay(5000);
            client.Connect();
        }


        private void Run()
        {
            client = new TwitchPubSub();

            client.OnPubSubServiceConnected += (s, e) =>
            {
                client.SendTopics();
                Console.WriteLine("PubSub connected");
            };
            client.OnListenResponse += (s, e) =>
            {
                if (!e.Successful) throw new Exception($"Failed to listen! Response: {e.Response}");
                else Console.WriteLine($"listening to {e.Topic}");
            };
            client.OnStreamDown += (s, e) =>
            {
                AskCommand.StreamOnline = false;
            };
            client.OnStreamUp += (s, e) =>
            {
                AskCommand.StreamOnline = true;
            };

            client.ListenToVideoPlayback(Bot.ChannelID.ToString());

            client.Connect();
        }
    }
}
