namespace StellarMeStream.Resources.Api.Twitch.Data;

public class IrcParsedCommand
{
    public bool IsCapRequestEnabled { get; set; }
    public string BotCommand { get; set; }
    public string BotCommandParams { get; set; }
    public string Channel { get; set; }
    public string CommandName { get; set; }
}
