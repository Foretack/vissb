using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Serilog;
using Timers = System.Timers;

namespace Core;
public static class AskCommand
{
    public static readonly HttpClient Requests = new();

    private static readonly string RequestLink = "https://api.openai.com/v1/engines/text-davinci-001/completions";
    private static readonly string[] BlacklistedUsers = { "titlechange_bot", "supibot", "streamelements", "megajumpbot" };
    private static readonly PriorityQueue<string, int> MessageQueue = new();
    private static readonly Dictionary<string, (string[], long)> PreviousContext = new();
    public static async ValueTask Run(string username, string prompt)
    {
        if (StreamMonitor.StreamOnline || BlacklistedUsers.Contains(username)) return;
        if (Cooldowns.OnCooldown(username))
        {
            MessageQueue.Enqueue($"{username} ? u coldown Okayeg", 100);
            return;
        }

        var body = new RequestBody
        {
            prompt = BuildContext(username, prompt),
            max_tokens = 90,
            temperature = 0.5f,
            top_p = 0.3f,
            frequency_penalty = 0.5f,
            presence_penalty = 0
        };

        string cString = JsonSerializer.Serialize(body);
        var content = new StringContent(cString, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await Requests.PostAsync(RequestLink, content);

        if (!response.IsSuccessStatusCode)
        {
            MessageQueue.Enqueue("eror Sadeg", 1);
            Log.Error("Request did not indicate success response (your token is likely consumed)");
            return;
        }

        Stream responseStream = await response.Content.ReadAsStreamAsync();
        string replyText = (await JsonSerializer.DeserializeAsync<ResponseBody>(responseStream))!.choices[0].text;

        if (replyText is null || replyText.Length < 3)
        {
            MessageQueue.Enqueue($"{username}, <empty message>", 50);
            return;
        }

        replyText = replyText[(replyText.IndexOf("\n\n") <= 0 ? 0 : replyText.IndexOf("\n\n"))..];
        replyText = replyText.ToLower().Contains(username) ? replyText : $"{username}, {replyText}";
        replyText = replyText.Filter();

        MessageQueue.Enqueue(replyText, 25);
        AddQNA(username, prompt, replyText);
        if (username != Config.Channel) Cooldowns.AddCooldown(username);
    }

    public static void HandleMessageQueue()
    {
        Timers::Timer queueTimer = new()
        {
            Interval = 3000,
            AutoReset = true,
            Enabled = true
        };

        queueTimer.Elapsed += (s, e) =>
        {
            if (MessageQueue.Count > 0)
                Bot.Client.SendMessage(Config.Channel, MessageQueue.Dequeue());
        };
    }

    private static string Filter(this string prompt)
    {
        if (prompt.Length > 450) prompt = prompt[..450] + "... (too long)";
        foreach (Regex regex in Regexes.Filters)
        {
            if (regex.IsMatch(prompt) && regex.Matches(prompt).Count == 1)
            {
                prompt = prompt.Replace(regex.Match(prompt).Value, " [Filtered] ");
            }
            // .Replace will not work on unique matches (e.g multiple IP addresses in the message). Hence the else if statement.
            else if (regex.IsMatch(prompt) && regex.Matches(prompt).Count > 1)
            {
                foreach (Match match in regex.Matches(prompt))
                {
                    prompt = prompt.Replace(match.Value, " [Filtered] ");
                }
            }
        }
        return prompt;
    }

    private static string BuildContext(string username, string prompt)
    {
        string newPrompt;
        bool s = PreviousContext.TryGetValue(username, out (string[], long) lastQuestionAndReply);

        if (!s)
        {
            newPrompt = $"{username}: {prompt} \n{Config.Username}: ";
            return newPrompt;
        }

        string lastQuestion = lastQuestionAndReply.Item1[0];
        string lastAnswer = lastQuestionAndReply.Item1[1];
        long lastReplyTime = lastQuestionAndReply.Item2;
        long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        newPrompt = $"{username}: {lastQuestion}\n{Config.Username}: {lastAnswer}\n{username}: {prompt} \n{Config.Username}: ";

        if (currentTime - lastReplyTime >= 300)
        {
            newPrompt = $"{username}: {prompt} \n{Config.Username}: ";
            return newPrompt;
        }

        return newPrompt;
    }

    private static void AddQNA(string username, string question, string answer)
    {
        bool s = PreviousContext.ContainsKey(username);

        if (!s)
        {
            PreviousContext.Add(username, (new string[] { question, answer }, DateTimeOffset.Now.ToUnixTimeSeconds()));
            return;
        }

        PreviousContext[username] = (new string[] { question, answer }, DateTimeOffset.Now.ToUnixTimeSeconds());
    }
}

public static class Cooldowns
{
    private static readonly List<string> CooldownPool = new();

    public static bool OnCooldown(string username) { return CooldownPool.Contains(username); }
    public static void AddCooldown(string username)
    {
        CooldownPool.Add(username);
        Timer? removalTimer = null;
        removalTimer = new Timer(callback =>
        {
            _ = CooldownPool.Remove(username);
            Log.Debug($"Removed {username} cooldown");
            removalTimer!.Dispose();
        }, null, Config.AskCommandCooldown * 1000, Timeout.Infinite);
    }
}

public static class Regexes
{
    public static readonly Regex[] Filters =
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
}

internal class RequestBody
{
    public string prompt { get; set; } = default!;
    public int max_tokens { get; set; }
    public float temperature { get; set; }
    public float top_p { get; set; }
    public float frequency_penalty { get; set; }
    public float presence_penalty { get; set; }
    public string[] stop { get; set; } = default!;
}

internal class ResponseBody
{
    public Choice[] choices { get; set; } = default!;

    public class Choice
    {
        public string text { get; set; } = default!;
    }
}
