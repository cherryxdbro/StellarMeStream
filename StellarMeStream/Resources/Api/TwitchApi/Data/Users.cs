using System.Text.Json.Serialization;

namespace StellarMeStream.Resources.Api.TwitchApi.Data;

internal class Users
{
    [JsonPropertyName("data")]
    public User[] UsersData { get; set; }
}
