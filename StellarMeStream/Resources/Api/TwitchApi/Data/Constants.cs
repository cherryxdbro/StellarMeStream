using SQLite;

namespace StellarMeStream.Resources.Api.TwitchApi.Data;

internal static class Constants
{
    internal const string DatabaseFilename = "StellarMeStream.sqlite3";

    internal const SQLiteOpenFlags Flags = SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    internal static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
}
