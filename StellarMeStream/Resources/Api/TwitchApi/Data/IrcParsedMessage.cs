namespace StellarMeStream.Resources.Api.TwitchApi.Data;

public class IrcParsedMessage
{
    public Dictionary<string, object> Tags { get; set; }
    public IrcParsedCommand Command { get; set; }
    public IrcParsedSource Source { get; set; }
    public string Parameters { get; set; }
}
