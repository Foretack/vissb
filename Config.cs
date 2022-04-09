namespace Core
{
    internal static class Config
    {
        /*
         * IMPORTANT: Your token must have the required scopes to see when a channel goes live.
         * If your token does not have the required scopes, it will always think the channel is 
         * offline and continue to reply.
         * 
         * You can generate a token with the required scopes here:
         * https://twitchtokengenerator.com/
         * 
         * choose custom scope token on the pop up, then scroll down until you see the 
         * [Select All] button. Press the button, then generate your token (from the 
         * [Generate Token] button on the right side of the [Select All] button).
         */
        public const string Username = "TWITCH_USERNAME";
        public const string Token = "ACCESS_TOKEN";
        public const string OpenAIToken = "Bearer OPEN_AI_AUTH";
        public const string Channel = "minusinsanity";
        public const int ChannelID = 17497365;
    }
}
