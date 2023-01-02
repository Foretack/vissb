namespace vissb;
public interface IAppConfig
{
    public string Username { get; }
    public string AccessToken { get; }
    public string ClientId { get; }
    public string Channel { get; }

    public string OpenAiToken { get; }
    public string RequestLink { get; }

    public int Cooldown { get; }
    public string OnCooldownMessage { get; }
    public int ContextSize { get; }
    public int SecondsUntilForgetContext { get; }
    public string ErrorMessage { get; }

    public string PingCommand { get; }
    public string ForgetCommand { get; }

    public int DailyTokenUsageLimit { get; }

    public bool Notify { get; }
    public int TokenThreshold { get; }
    public string NotifyWebhookLink { get; }
}
