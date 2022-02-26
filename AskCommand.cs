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

        public AskCommand()
        {
            HandleMessageQueue();
        }

        public static async Task RunCommand(string Username, string Input)
        {
            if (StreamOnline) { return; }
            if (Cooldown.OnCooldown(Username).Item1) 
            { 
                Messages.Enqueue($"@{Username}, forsenDonk Wait {Cooldown.OnCooldown(Username).Item2}s"); 
                return; 
            }

            RequestBody body = new()
            {
                prompt = $"{Username} asks {Bot.Username}: {Input} \n{Bot.Username}:",
                max_tokens = 90,
                temperature = 0.5f,
                top_p = 0.3f,
                frequency_penalty = 0.5f,
                presence_penalty = 0.0f,
                stop = new string[] { "You:", $"{Username}:" }
            };

            string contentAsString = JsonConvert.SerializeObject(body);
            StringContent content = new(contentAsString, Encoding.UTF8, "application/json");
            HttpResponseMessage req = await Requests.PostAsync(APILink, content);

            if (req.StatusCode != HttpStatusCode.OK) 
            {
                Messages.Enqueue("eror Sadeg");
                return; 
            }

            string result = req.Content.ReadAsStringAsync().Result;
            ResponseBody response = JsonConvert.DeserializeObject<ResponseBody>(result) ?? throw new Exception();
            string reply = $"@{Username}, <no response>";

            if (response.choices.First().text.Length < 2) 
            {
                Messages.Enqueue(Filter(reply));
                Cooldown.AddCooldown(Username);
                return; 
            }

            string replyText = response.choices.First().text;
            reply = $"{replyText.Substring((replyText.IndexOf("\n\n") < 0 ? 0 : replyText.IndexOf("\n\n")))}";
            reply = (reply.ToLower().Contains(Username)) ? reply : $"{Username}, {reply}";
            Messages.Enqueue(Filter(reply));
            if (Username == Bot.Channel) return;
            Cooldown.AddCooldown(Username);
        }

        static readonly Regex XD = new(@"(\b(negro|coon)\b)");
        static readonly Regex NoBruhMoments = new(@"(?:(?:\b(?<![-=\.])|monka)(?:[Nnñ]|[Ii7]V)|η|[\/|]\\[\/|])[\s\.]*?[liI1y!j\/|]+[\s\.]*?(?:[GgbB6934Q🅱qğĜƃ၅5\*][\s\.]*?){2,}(?!arcS|l|Ktlw|ylul|ie217|64|\d? ?times)");
        static readonly Regex NotTwelve = new(@"(\b[iI][012]\b|\b[1-9]\b|\b1[012]\b|twelve|eleven|ten|nine|eight|seven|six|five|four|three|two|one).*year(s)?.*(old|age)");
        static readonly Regex NoLinks = new(@"(https:[\\/][\\/])?(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&\=]*)");
        static readonly Regex NoIps = new(@"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.|$)){4}");

        private static string Filter(string Input)
        {
            string output = Input;

            if (NoIps.Match(output.Remove(output.Length - 1)).Success) output = Input.Replace(NoIps.Match(Input).Value, " BigTrouble ");
            if (NoBruhMoments.Match(output).Success) output = Input.Replace(NoBruhMoments.Match(Input).Value, " Uhmgi ");
            if (NoLinks.Match(output).Success) output = Input.Replace(NoLinks.Match(output).Value, " MODS [LINK] ");
            if (NotTwelve.Match(output).Success) output = Input.Replace(NotTwelve.Match(Input).Value, " YOURM0M ");
            if (output.Length > 495) output = output.Substring(0, 460) + "... (too long)";

            return output;
        }

        private static Queue<string> Messages = new();

        private static void HandleMessageQueue()
        {
            System.Timers.Timer queueTimer = new();

            queueTimer.Interval = 3000;
            queueTimer.AutoReset = true;
            queueTimer.Enabled = true;

            queueTimer.Elapsed += (s, e) =>
            {
                if (Messages.Count > 0)
                {
                    Bot.client.SendMessage(Bot.Channel, Messages.Dequeue());
                }
            };
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
        public float top_p { get; set; }
        public float frequency_penalty { get; set; }
        public float presence_penalty { get; set; }
        public string[] stop { get; set; }
    }

    class ResponseBody
    {
        public string id { get; set; }
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
