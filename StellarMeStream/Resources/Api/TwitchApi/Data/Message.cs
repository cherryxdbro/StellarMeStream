using SQLite;
using SQLiteNetExtensions.Attributes;

namespace StellarMeStream.Resources.Api.TwitchApi.Data;

[Table("Messages")]
public class Message
{
    [PrimaryKey]
    public string Id { get; set; }
    [ForeignKey(typeof(User))]
    public string UserId { get; set; }
    [ManyToOne]
    public User User { get; set; }
    public string Data { get; set; }
    public string ReceivingTime { get; set; } = DateTime.Now.ToString();
}
