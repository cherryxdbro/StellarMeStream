namespace StellarMeStream.Resources.Api.TwitchApi.Data;

internal class Channel
{
    internal AccessToken AccessToken { get; set; } = new();

    internal string ClientId { get; set; }
    internal string ClientSecret { get; set; }
    internal string Code { get; set; }

    internal User UserData { get; set; } = new();
}
