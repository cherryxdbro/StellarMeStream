using SQLite;

namespace StellarMeStream.Resources.Api.TwitchApi.Data;

[Table("Messages")]
internal class Message
{
    [PrimaryKey]
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Data { get; set; }
    public DateTime ReceivingTime { get; set; } = DateTime.Now;
}
