using System.Text;

namespace StellarMeStream.Resources.Api.Twitch;

internal static class TwitchChatMessageHandler
{
    internal enum ChatMessageAction
    {
        None,
        Ban,
        Timeout,
        Super
    }

    private const int MaxMessageLength = 300;

    private static readonly List<string> BanWords =
    [
        "негр",
        "hегр",
        "heгр",
        "hegр",
        "hegr",
        "nегр",
        "neгр",
        "negр",
        "negr",
        "нegr",
        "неgr",
        "негr",
        "нegр",
        "нeгр",
        "неgр",

        "нигер",
        "hигер",
        "hiгер",



        "п€дик",
        "retard",
        "faggot",
        "нага",
        "simp",
        "incel",
        "cимп",

        "hiгер",
        "hiгер",
        "hiгер",
        "hiгер",
        "hiгер",
        "hiгер",
        "hiгер",
        "hiгер",
        "hiгер",



        "нига",
        "hига",
        "hiга",

        "nидор",
        "пидор",
        "пидар",
        "пидер",

        "пидр",
        "nидр",

        "педик",

        "даун",
        "daun",

        "жид",
        "zhid",

        "симп",
        "simp",

        "хач",
        "hach",

        "хохол",
        "hohol",

        "инцел",

        "аутист",
        "додик",
        "даун",
        "daun",

        "гомик",
        "гитлер",
        "hitler",
        "gitler",

        "куколд"
    ];
    private static readonly List<string> MuteWords =
    [
        "1488"
    ];
    public static readonly List<string> SuperWords =
    [
        //"пригожин",
        //"тг золо",
        //"микро",
        //"звук",
        //"душит",
        //"душно",
        //"душнила",
        //"скучно",
        //"асу",
        //"артем",
        //"артём",
        //"граф",
        //"душно",
        //"духота",
        //"афк",
        //"обернись",
        //"скип",
    ];
    public static HashSet<char> StrangeSymbols =
    [
        '⣿',
        '⠉',
        '⠛',
        '⣶',
        '⣤',
        '█',
        '▒',
        '░',
    ];

    internal static ChatMessageAction IsMessageBad(string message)
    {
        if (message.Length > MaxMessageLength)
        {
            return ChatMessageAction.Timeout;
        }
        string cleanedMessage = RemoveDuplicateCharacters(message);
        if (ContainsStrangeSymbols(cleanedMessage))
        {
            return ChatMessageAction.Timeout;
        }
        if (ContainsWords(cleanedMessage, BanWords))
        {
            return ChatMessageAction.Ban;
        }
        if (ContainsWords(cleanedMessage, MuteWords))
        {
            return ChatMessageAction.Timeout;
        }
        if (ContainsWords(cleanedMessage, SuperWords))
        {
            return ChatMessageAction.Super;
        }
        return ChatMessageAction.None;
    }

    private static string RemoveDuplicateCharacters(string message)
    {
        StringBuilder uniqueChars = new();
        foreach (char currentChar in message)
        {
            if (uniqueChars.Length == 0 || uniqueChars[^1] != currentChar)
            {
                uniqueChars.Append(currentChar);
            }
        }
        return uniqueChars.ToString();
    }

    private static bool ContainsWords(string message, List<string> words)
    {
        foreach (string word in words)
        {
            if (message.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static bool ContainsStrangeSymbols(string message)
    {
        foreach (char currentChar in message)
        {
            if (StrangeSymbols.Contains(currentChar))
            {
                return true;
            }
        }
        return false;
    }
}
