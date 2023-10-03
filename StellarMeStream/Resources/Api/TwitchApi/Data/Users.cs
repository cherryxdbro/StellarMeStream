using System.Text.Json.Serialization;

namespace StellarMeStream.Resources.Api.TwitchApi.Data;

public class Users
{
    [JsonPropertyName("data")]
    public User[] UsersData { get; set; }
}
