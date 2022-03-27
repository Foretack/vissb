﻿using System;
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
        private static readonly string APILink = "https://api.openai.com/v1/engines/curie/completions";
        private static readonly string[] BlacklistedUsers = { "titlechange_bot", "supibot", "streamelements" };

        public AskCommand()
        {
            HandleMessageQueue();
        }

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
                prompt = $"{Input}",
                max_tokens = 90,
                temperature = 0.5f,
                top_p = 0.3f,
                frequency_penalty = 0.5f,
                presence_penalty = 0.0f,
                stop = new string[] { $"{Username}:", $"{Bot.Username}:" }
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

            if (response.choices.First().text.Length < 2) 
            {
                Messages.Enqueue(await Filter(reply), 50);
                Cooldown.AddCooldown(Username);
                return; 
            }

            string replyText = response.choices.First().text;
            reply = $"{replyText.Substring((replyText.IndexOf("\n\n") < 0 ? 0 : replyText.IndexOf("\n\n")))}";
            reply = reply.ToLower().Contains(Username) ? reply : $"{Username}, {reply}";
            Messages.Enqueue(await Filter(reply), 50);
            // Don't add a cooldown to Broadcaster.
            if (Username == Bot.Channel) return;
            Cooldown.AddCooldown(Username);
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
            string output = Input;
            await Task.Run(() =>
            {
                // Do this first to reduce work on regexes
                if (output.Length > 450) output = output.Substring(0, 450) + "... (too long)";
                foreach (Regex regex in Filters)
                {
                    if (regex.IsMatch(output) && regex.Matches(output).Count == 1) output = output.Replace(regex.Match(output).Value, " [Filtered] ");
                    // .Replace will not work on unique matches (e.g multiple IP addresses in the message). Hence the else if statement.
                    else if (regex.IsMatch(output) && regex.Matches(output).Count > 1)
                    {
                        foreach (Match match in regex.Matches(output))
                        {
                            output = output.Replace(match.Value, " [Filtered] "); // Nesting FeelsGoodMan
                        }
                    }
                }
            });
            return output;
        }

        // Lower int value = Higher priority
        private static PriorityQueue<string, int> Messages = new();

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
        public static Dictionary<string, long> CooldownPool = new();

        public static void AddCooldown(string User)
        {
            // .Add shouldn't cause an exception but it does
            CooldownPool.TryAdd(User, DateTimeOffset.Now.ToUnixTimeSeconds());
        }

        // Checks if the user is on cooldown. Returns (false, null) if not.
        // Else returns (true, cooldown in seconds)
        public static ValueTuple<bool, int?> OnCooldown(string User)
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
