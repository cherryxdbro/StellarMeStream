using System.Collections.Concurrent;

namespace StellarMeStream.Resources.Api.TwitchApi.Data;

internal class Connection
{
    internal Channel Channel { get; set; } = new();

    internal ConcurrentDictionary<string, Channel> TargetChannels { get; set; } = new();
}
