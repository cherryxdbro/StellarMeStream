using System.Text.Json.Serialization;

namespace StellarMeStream.Resources.Api.Twitch.Data;

public class Users
{
    [JsonPropertyName("data")]
    public User[] UsersData { get; set; }
}
