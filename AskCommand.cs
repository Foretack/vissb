using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Core
{
    public class AskCommand
    {
        public static bool StreamOnline = false;
        public static HttpClient Requests = new();

        private static readonly string AuthorizationString = "Bearer OPENAI_TOKEN_GOES_HERE";
        private static readonly string APILink = "https://api.openai.com/v1/engines/text-davinci-001/completions";

        public static async Task RunCommand(string Username, string Input)
        {
            if (StreamOnline) { Bot.client.SendMessage(Bot.Channel, "Okayeg 💢 u nab? strem on"); return; }
            if (Cooldown.OnCooldown(Username).Item1) { Bot.client.SendMessage(Bot.Channel, $"@{Username}, forsenDonk Wait {Cooldown.OnCooldown(Username).Item2}s"); return; }

            RequestBody body = new()
            {
                prompt = Input,
                max_tokens = 160,
                temperature = 0.7f,
                frequency_penalty = 0.5f
            };

            string contentAsString = JsonConvert.SerializeObject(body);
            StringContent content = new(contentAsString, Encoding.UTF8, "application/json");
            Requests.DefaultRequestHeaders.Add("Authorization", AuthorizationString);
            HttpResponseMessage req = await Requests.PostAsync(APILink, content);
            Requests.DefaultRequestHeaders.Remove("Authorization");

            if (req.StatusCode != HttpStatusCode.OK) { Bot.client.SendMessage(Bot.Channel, "eror Sadeg"); return; }

            ResponseBody response = JsonConvert.DeserializeObject<ResponseBody>(req.Content.ReadAsStringAsync().Result) ?? throw new Exception("This should not happen");
            string reply = $"@{Username}, <no response>";

            if (response.choices.Length == 0) { Bot.client.SendMessage(Bot.Channel, reply); Cooldown.AddCooldown(Username); return; }

            reply = $"@{Username}, {response.choices.First().text.Substring(1)}";
            Bot.client.SendMessage(Bot.Channel, reply);
            Cooldown.AddCooldown(Username);
        }
    }

    public class Cooldown
    {
        public static List<ValueTuple<string, long>> CooldownPool = new();

        public static void AddCooldown(string User)
        {
            CooldownPool.Add((User, DateTimeOffset.Now.ToUnixTimeSeconds()));
        }

        public static ValueTuple<bool, int?> OnCooldown(string User)
        {
            if (CooldownPool.Count == 0) return (false, null);

            foreach (var cdt in CooldownPool)
            {
                string name = cdt.Item1;
                long lastUsed = cdt.Item2;

                if (name == User)
                {
                    if (DateTimeOffset.Now.ToUnixTimeSeconds() - lastUsed < 59)
                    {
                        return (true, (int)(60 - (DateTimeOffset.Now.ToUnixTimeSeconds() - lastUsed)));
                    }
                    else { CooldownPool.Remove(cdt); return (false, null); }
                }
            }

            return (false, null);
        }
    }

#pragma warning disable CS8618

    class RequestBody
    {
        public string prompt { get; set; }
        public int max_tokens { get; set; }
        public float temperature { get; set; }
        public float frequency_penalty { get; set; }
    }

    class ResponseBody
    {
        public string id { get; set; }
        [JsonProperty("object")]
        public string @object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }

        public class Choice
        {
            public string text { get; set; }
            public int index { get; set; }
            public int? logprobs { get; set; }
            public string? finish_reason { get; set; }
        }
    }
}
