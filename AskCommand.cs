using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Core
{
    public static class AskCommand
    {
        public static bool StreamOnline { get; set; } = false;
        public static HttpClient Requests { get; } = new();

        private static string APILink { get; } = "https://api.openai.com/v1/engines/text-davinci-001/completions";
        private static string[] BlacklistedUsers { get; } = { "titlechange_bot", "supibot", "streamelements", "megajumpbot" };
        private static Stack<string[]> MessageHistory { get; } = new();
        private static PriorityQueue<string, int> Messages { get; } = new();

        public static async Task RunCommand(string Username, string Input)
        {
            if (StreamOnline || BlacklistedUsers.Contains(Username)) return;
            if (Cooldown.OnCooldown(Username).Item1)
            {
                Messages.Enqueue($"@{Username}, ppHop Wait {Cooldown.OnCooldown(Username).Item2}s", 100);
                return;
            }

            RequestBody body = new()
            {
                prompt = BuildContext(Username, Input),
                max_tokens = 90,
                temperature = 0.5f,
                top_p = 0.3f,
                frequency_penalty = 0.5f,
                presence_penalty = 0.0f,
            };

            string contentAsString = JsonConvert.SerializeObject(body);
            StringContent content = new(contentAsString, Encoding.UTF8, "application/json");
            HttpResponseMessage req = await Requests.PostAsync(APILink, content);

            if (req.StatusCode != HttpStatusCode.OK)
            {
                Messages.Enqueue("eror Sadeg", 75);
                return;
            }

            string result = await req.Content.ReadAsStringAsync();
            ResponseBody response = JsonConvert.DeserializeObject<ResponseBody>(result)!;
            string reply = $"@{Username}, <no response>";

            if (response.choices.First().text.Length < 3)
            {
                Messages.Enqueue(await Filter(reply), 50);
                Cooldown.AddCooldown(Username);
                return;
            }

            string replyText = response.choices.First().text;
            reply = $"{replyText.Substring((replyText.IndexOf("\n\n") < 0 ? 0 : replyText.IndexOf("\n\n")))}";
            reply = reply.ToLower().Contains(Username) ? reply : $"{Username}, {reply}";
            reply = await Filter(reply);
            Messages.Enqueue(reply, 50);
            AddQNA(Username, Input, reply);
            // Don't add a cooldown to Broadcaster.
            if (Username == Config.Channel) return;
            Cooldown.AddCooldown(Username);
        }

        private static string BuildContext(string username, string prompt)
        {
            StringBuilder sb = new();
            foreach (string[] qna in MessageHistory.AsEnumerable())
            {
                sb.Append($"{qna[0]}: {qna[1]}\n");
            }
            sb.Append($"{username}: {prompt}\n");
            sb.Append($"{Config.Username}: ");

            return sb.ToString();
        }

        private static void AddQNA(string username, string question, string answer)
        {
            MessageHistory.Push(new string[] { username, question });
            MessageHistory.Push(new string[] { Config.Username, answer });

            if (MessageHistory.Count > 15)
            {
                MessageHistory.Pop();
                MessageHistory.Pop();
            }
        }

        private static readonly Regex[] Filters =
        {
            // N words
            new Regex(@"(?:(?:\b(?<![-=\.])|monka)(?:[Nnñ]|[Ii7]V)|η|[\/|]\\[\/|])[\s\.]*?[liI1y!j\/|]+[\s\.]*?(?:[GgbB6934Q🅱qğĜƃ၅5\*][\s\.]*?){2,}(?!arcS|l|Ktlw|ylul|ie217|64|\d? ?times)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)),
            // Age TOS
            new Regex(@"(\b[iI][012]\b|\b[1-9]\b|\b1[012]\b|twelve|eleven|ten|nine|eight|seven|six|five|four|three|two|one).*year(s)?.*(old|age)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)),
            // Links
            new Regex(@"(https:[\\/][\\/])?(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&\=]*)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)),
            // (Valid) IP addresses
            new Regex(@"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.|$)){4}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)),
            // More TOS hunting
            new Regex(@"\b(negro|coon)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200))
        };

        private static async Task<string> Filter(string Input)
        {
            await Task.Run(() =>
            {
                // Do this first to reduce work on regexes
                if (Input.Length > 450) Input = Input.Substring(0, 450) + "... (too long)";
                foreach (Regex regex in Filters)
                {
                    if (regex.IsMatch(Input) && regex.Matches(Input).Count == 1) Input = Input.Replace(regex.Match(Input).Value, " [Filtered] ");
                    // .Replace will not work on unique matches (e.g multiple IP addresses in the message). Hence the else if statement.
                    else if (regex.IsMatch(Input) && regex.Matches(Input).Count > 1)
                    {
                        foreach (Match match in regex.Matches(Input))
                        {
                            Input = Input.Replace(match.Value, " [Filtered] ");
                        }
                    }
                }
            });
            return Input;
        }


        public static void HandleMessageQueue()
        {
            System.Timers.Timer queueTimer = new();

            queueTimer.Interval = 3000;
            queueTimer.AutoReset = true;
            queueTimer.Enabled = true;

            queueTimer.Elapsed += (s, e) =>
            {
                if (Messages.Count > 0)
                {
                    Bot.Client.SendMessage(Config.Channel, Messages.Dequeue());
                }
            };
        }
    }

    public static class Cooldown
    {
        private static Dictionary<string, long> CooldownPool { get; } = new();

        public static void AddCooldown(string User)
        {
            CooldownPool.TryAdd(User, DateTimeOffset.Now.ToUnixTimeSeconds());
        }

        // Checks if the user is on cooldown. Returns (false, null) if not.
        // Else returns (true, cooldown in seconds)
        public static (bool, int?) OnCooldown(string User)
        {
            if (CooldownPool.Count == 0) return (false, null);

            bool s = CooldownPool.TryGetValue(User, out long lastUsed);

            if (!s) return (false, null);
            if (DateTimeOffset.Now.ToUnixTimeSeconds() - lastUsed < 59)
            {
                return (true, (int)(60 - (DateTimeOffset.Now.ToUnixTimeSeconds() - lastUsed)));
            }

            CooldownPool.Remove(User);

            return (false, null);
        }
    }

    class RequestBody
    {
        public string prompt { get; set; } = default!;
        public int max_tokens { get; set; }
        public float temperature { get; set; }
        public float top_p { get; set; }
        public float frequency_penalty { get; set; }
        public float presence_penalty { get; set; }
        public string[] stop { get; set; } = default!;
    }

    class ResponseBody
    {
        public Choice[] choices { get; set; } = default!;

        public class Choice
        {
            public string text { get; set; } = default!;
        }
    }
}
