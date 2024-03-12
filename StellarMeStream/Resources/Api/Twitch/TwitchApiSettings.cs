using StellarMeStream.Resources.Api.Twitch.Data;
using System.Collections.Concurrent;

namespace StellarMeStream.Resources.Api.Twitch;

internal static class TwitchApiSettings
{
    internal static ConcurrentDictionary<string, Connection> TwitchConnections { get; set; } = new();
}
