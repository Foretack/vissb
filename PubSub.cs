using System;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace Core
{
    public class PubSub
    {
        private TwitchPubSub? client;

        public PubSub()
        {
            Run();
        }

        private void Run()
        {
            client = new TwitchPubSub();

            client.OnPubSubServiceConnected += (s, e) =>
            {
                client.SendTopics();
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
            client.OnViewCount += (s, e) =>
            {
                AskCommand.StreamOnline = true;
            };

            client.ListenToVideoPlayback("17497365");

            client.Connect();
        }
    }
}
