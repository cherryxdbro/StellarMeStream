using SQLite;
using StellarMeStream.Resources.Api.TwitchApi.Data;

namespace StellarMeStream;

internal static class StellarMeStreamDatabase
{
    private const string DatabaseFilename = "StellarMeStream.sqlite3";
    private const SQLiteOpenFlags Flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    private static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

    internal static SQLiteAsyncConnection CurrentInstance { get; private set; }

    internal static async Task<CreateTablesResult> Initialize() => await (CurrentInstance ??= new SQLiteAsyncConnection(DatabasePath, Flags)).CreateTablesAsync<User, Message>();
}
