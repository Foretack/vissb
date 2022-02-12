using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Core
{
    public class AskCommand
    {
        public static bool StreamOnline = false;
        public static HttpClient Requests = new();
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
            HttpResponseMessage req = await Requests.PostAsync(APILink, content);

            if (req.StatusCode != HttpStatusCode.OK) { Bot.client.SendMessage(Bot.Channel, "eror Sadeg"); return; }

            ResponseBody response = JsonConvert.DeserializeObject<ResponseBody>(req.Content.ReadAsStringAsync().Result) ?? throw new Exception("This should not happen");
            string reply = $"@{Username}, <no response>";

            if (response.choices.Length == 0) { Bot.client.SendMessage(Bot.Channel, reply); Cooldown.AddCooldown(Username); return; }

            reply = $"@{Username}, {response.choices.First().text.Substring(1)}";
            Bot.client.SendMessage(Bot.Channel, Filter(reply));
            Cooldown.AddCooldown(Username);
        }

        private static readonly Regex NotTwelve = new(@"(\b[1-9]\b|\b1[012]\b|twelve|eleven|ten|nine|eight|seven|six|five|four|three|two|one).*year(s)?.*(old|age)");
        private static string Filter(string Input)
        {
            string output = Input;

            if (NotTwelve.Match(Input).Success) output = Input.Replace(NotTwelve.Match(Input).Value, " YOURM0M ");

            return output;
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
