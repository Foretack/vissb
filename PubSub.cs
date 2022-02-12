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
                Bot.client.SendMessage(Bot.Channel, "billyArrive channel offline");
                AskCommand.StreamOnline = false;
            };
            client.OnViewCount += (s, e) =>
            {
                if (AskCommand.StreamOnline == false) Bot.client.SendMessage(Bot.Channel, "billyLeave channel online");
                AskCommand.StreamOnline = true;
            };

            client.ListenToVideoPlayback(Bot.ChannelID.ToString());

            client.Connect();
        }
    }
}
