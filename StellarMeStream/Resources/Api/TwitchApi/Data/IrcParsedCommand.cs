namespace StellarMeStream.Resources.Api.TwitchApi.Data;

internal class IrcParsedCommand
{
    internal bool IsCapRequestEnabled { get; set; }
    internal string BotCommand { get; set; }
    internal string BotCommandParams { get; set; }
    internal string Channel { get; set; }
    internal string CommandName { get; set; }
}
