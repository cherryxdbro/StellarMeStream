namespace StellarMeStream.Resources.Api.Surreal.Data;

public class Timer : SurrealDb.Net.Models.Record
{
    public int count { get; set; }
    public bool enabled { get; set; }
    public string message { get; set; }
    public TimeSpan offset { get; set; }
    public TimeSpan period { get; set; }
}
