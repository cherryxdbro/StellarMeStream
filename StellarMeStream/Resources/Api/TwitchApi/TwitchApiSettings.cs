using StellarMeStream.Resources.Api.TwitchApi.Data;
using System.Collections.Concurrent;

namespace StellarMeStream.Resources.Api.TwitchApi;

internal static class TwitchApiSettings
{
    internal static ConcurrentDictionary<string, Connection> TwitchConnections { get; set; } = new();
}
