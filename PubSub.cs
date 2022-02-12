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
            client.OnStreamUp += (s, e) => 
            {
                Console.WriteLine($"Stream just went up! Play delay: {e.PlayDelay}, server time: {e.ServerTime}");
            };
            client.OnStreamDown += (s, e) =>
            {
                Console.WriteLine($"Stream just went down! Server time: {e.ServerTime}");
            };
            client.OnFollow += (s, e) =>
            {
                Console.WriteLine($"{e.Username} just followed!");
            };
            client.OnViewCount += (s, e) =>
            {
                Console.WriteLine($"{e.Viewers} {e.ServerTime}");
            };

            client.ListenToVideoPlayback("17497365");
            client.ListenToFollows("17497365");

            client.Connect();
        }
    }
}
