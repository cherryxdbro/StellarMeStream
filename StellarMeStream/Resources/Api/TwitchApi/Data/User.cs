using SQLite;
using SQLiteNetExtensions.Attributes;
using System.Text.Json.Serialization;

namespace StellarMeStream.Resources.Api.TwitchApi.Data;

[Table("Users")]
public class User
{
    [JsonPropertyName("id"), PrimaryKey]
    public string Id { get; set; }
    [JsonPropertyName("login")]
    public string Login { get; set; }
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("broadcaster_type")]
    public string BroadcasterType { get; set; }
    [JsonPropertyName("description")]
    public string Description { get; set; }
    [JsonPropertyName("profile_image_url")]
    public string ProfileImageUrl { get; set; }
    [JsonPropertyName("offline_image_url")]
    public string OfflineImageUrl { get; set; }
    [JsonPropertyName("email")]
    public string Email { get; set; }
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; }
    [OneToMany(CascadeOperations = CascadeOperation.All)]
    public List<Message> Messages { get; set; }
    public string ReceivingTime { get; set; } = DateTime.Now.ToString();
}
