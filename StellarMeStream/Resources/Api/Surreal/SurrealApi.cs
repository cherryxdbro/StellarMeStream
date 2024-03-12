namespace StellarMeStream.Resources.Api.Surreal;

internal static class SurrealApi
{
    private static SurrealDb.Net.SurrealDbClient db;

    public static async Task Initialize()
    {
        db = new("ws://188.225.74.224:6400/rpc");
        await db.SignIn(new SurrealDb.Net.Models.Auth.RootAuth { Username = "main", Password = "cf59c93d-1140-4aa1-bda2-4720601e089b" });
        await db.Use("main", "main");
    }
}
