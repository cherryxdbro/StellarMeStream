using System.Text.Json.Serialization;

namespace StellarMeStream.Resources.Api.Twitch.Data;

public class AccessToken
{
    [JsonPropertyName("access_token")]
    public string Token { get; set; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }
    [JsonPropertyName("scope")]
    public string[] Scope { get; set; }
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }
    public string UpdatedAt { get; set; } = DateTime.Now.ToString();
}
