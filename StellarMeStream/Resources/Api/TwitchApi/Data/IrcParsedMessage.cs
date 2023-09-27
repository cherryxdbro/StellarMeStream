namespace StellarMeStream.Resources.Api.TwitchApi.Data;

internal class IrcParsedMessage
{
    internal Dictionary<string, object> Tags { get; set; }
    internal IrcParsedCommand Command { get; set; }
    internal IrcParsedSource Source { get; set; }
    internal string Parameters { get; set; }
}
