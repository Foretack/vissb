﻿using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Serilog;

namespace vissb;
internal static class OpenAiInteraction
{
    private static readonly string _requestLink = ConfigLoader.AppConfig.RequestLink;
    private static readonly Dictionary<string, Queue<Conversation>> _conversations = new();
    private static readonly Dictionary<string, DateTime> _lastUsed = new();
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public static async Task<(string, byte, int)> Complete(string username, string prompt)
    {
        if (!_http.DefaultRequestHeaders.Contains("Authorization"))
            _http.DefaultRequestHeaders.Add("Authorization", ConfigLoader.AppConfig.OpenAiToken);
        if (IsOnCooldown(username)) return (username + ' ' + ConfigLoader.AppConfig.OnCooldownMessage, 10, 0);

        var reqObj = new
        {
            prompt = ContextFrom(username, prompt),
            max_tokens = 90,
            temperature = RandomF(),
            top_p = RandomF(),
            frequency_penalty = RandomF(),
            presence_penalty = 0,
        };

        HttpResponseMessage post;

        try
        {
            post = await _http.PostAsJsonAsync(_requestLink, reqObj);
        }
        catch (TimeoutException)
        {
            Log.Warning("Request timed out");
            return ($"{username}, {ConfigLoader.AppConfig.ErrorMessage} (timed out)", 1, 0);
        }
        catch (Exception ex)
        {
            Log.Error("Request failed", ex);
            return ($"{username}, {ConfigLoader.AppConfig.ErrorMessage} ({ex.Message})", 1, 0);
        }

        if (!post.IsSuccessStatusCode)
        {
            Log.Warning("Request failed ({code})", post.StatusCode);
            return ($"{username}, {ConfigLoader.AppConfig.ErrorMessage} ({post.StatusCode})", 1, 0);
        }

        var response = await post.Content.ReadFromJsonAsync<OpenAiResponse>();
        if (response is null)
        {
            Log.Warning("Request failed (failed to serialize)");
            return ($"{username}, {ConfigLoader.AppConfig.ErrorMessage} (failed to serialize)", 1, 0);
        }

        string replyRaw = response.Choices[0].Text;
        int start = replyRaw.IndexOf("\n\n");
        string replyText =
            (replyRaw.ToLower().Contains(username)
                ? string.Empty
                : username + ", ")
            + replyRaw[start <= 0 || start > 10 ? 0.. : start..];

        if (replyText.Length > 475)
            replyText = replyText[..475] + " ... (too long)";
        replyText = Filter(replyText);

        AddConversation(username, (prompt, replyText));
        if (username != ConfigLoader.AppConfig.Channel)
            AddCooldown(username);

        return (replyText, 25, response.Usage.TotalTokens);
    }

    private static string ContextFrom(string username, string prompt)
    {
        if (_lastUsed.TryGetValue(username, out var time))
        {
            if ((DateTime.Now - time).TotalSeconds > ConfigLoader.AppConfig.SecondsUntilForgetContext)
                _ = _conversations.Remove(username);
        }
        if (!_conversations.ContainsKey(username))
        {
            return $"\n{username}: {prompt}\n{ConfigLoader.AppConfig.Username}: ";
        }

        var built = string.Join('\n', _conversations[username]
                .Where(x => x is not null)
                .Select(x => $"{username}: {x.Question}\n{ConfigLoader.AppConfig.Username}: {x.Response}"));

        return built + $"\n{username}: {prompt}\n{ConfigLoader.AppConfig.Username}: ";
    }

    public static void ForgetContex(string username)
    {
        _ = _conversations.Remove(username);
    }

    private static bool IsOnCooldown(string username)
    {
        if (_lastUsed.TryGetValue(username, out var time))
        {
            if ((DateTime.Now - time).TotalSeconds < ConfigLoader.AppConfig.Cooldown) return true;
        }
        return false;
    }

    private static string Filter(string text)
    {
        foreach (var f in Filters)
        {
            text = f.Replace(text, "[Filtered]");
        }

        return text;
    }

    private static void AddConversation(string username, Conversation convo)
    {
        if (_conversations.TryGetValue(username, out var queue))
        {
            if (queue.Count == ConfigLoader.AppConfig.ContextSize)
            {
                _ = queue.Dequeue();
            }
            queue.Enqueue(convo);
            return;
        }
        _conversations.Add(username, new());
        _conversations[username].Enqueue(convo);
    }

    private static void AddCooldown(string username)
    {
        if (_lastUsed.ContainsKey(username))
        {
            _lastUsed[username] = DateTime.Now;
            return;
        }
        _lastUsed.Add(username, DateTime.Now);
    }

    private static readonly Regex[] Filters =
    {
        new(@"(?:(?:\b(?<![-=\.])|monka)(?:[Nnñ]|[Ii7]V)|η|[\/|]\\[\/|])[\s\.]*?[liI1y!j\/|]+[\s\.]*?(?:[GgbB6934Q🅱qğĜƃ၅5\*][\s\.]*?){2,}(?!arcS|l|Ktlw|ylul|ie217|64|\d? ?times)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)),
        new(@"(\b[iI][012]\b|\b[1-9]\b|\b1[012]\b|twelve|eleven|ten|nine|eight|seven|six|five|four|three|two|one).*year(s)?.*(old|age)", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)),
        new(@"https?:[\\/][\\/](www\.|[-a-zA-Z0-9]+\.)?[-a-zA-Z0-9@:%._\+~#=]{3,}(\.[a-zA-Z]{2,})+(/([-a-zA-Z0-9@:%._\+~#=/?&]+)?)?\b", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)),
        new(@"\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\.|$)){4}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200)),
        new(@"\b(negro|coon)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(200))
    };

    private static float RandomF() => Random.Shared.NextSingle();
}

internal sealed record Conversation(string Question, string Response)
{
    public static implicit operator Conversation((string, string) tuple)
        => new(tuple.Item1, tuple.Item2);
};
