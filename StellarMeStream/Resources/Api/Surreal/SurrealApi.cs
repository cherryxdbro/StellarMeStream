namespace StellarMeStream.Resources.Api.Surreal;

internal static class SurrealApi
{
    internal static readonly SurrealDb.Net.SurrealDbClient SurrealDbClient = new(new SurrealDbOptionsBuilder().WithEndpoint("ws://188.225.74.224:6400/rpc").WithNamespace("main").WithDatabase("main").WithUsername("main").WithPassword("cf59c93d-1140-4aa1-bda2-4720601e089b").Build());

    internal static async void Initialize()
    {
        await SurrealDbClient.Query($"define table if not exists timers schemafull;define field if not exists count on table timers type int;define field if not exists enabled on table timers type bool;define field if not exists message on table timers type string;define field if not exists offset on table timers type duration;define field if not exists period on table timers type duration;define index if not exists timers_message_index on timers fields message unique;");
    }
}
