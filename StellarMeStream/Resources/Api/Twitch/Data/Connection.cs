using System.Collections.Concurrent;

namespace StellarMeStream.Resources.Api.Twitch.Data;

internal class Connection
{
    internal Channel Channel { get; set; } = new();

    internal ConcurrentDictionary<string, Channel> TargetChannels { get; set; } = new();
}
