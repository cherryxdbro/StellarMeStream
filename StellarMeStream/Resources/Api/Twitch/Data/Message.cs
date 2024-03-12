namespace StellarMeStream.Resources.Api.Twitch.Data;

public class Message
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Data { get; set; }
    public string ReceivingTime { get; set; } = DateTime.Now.ToString();
}
