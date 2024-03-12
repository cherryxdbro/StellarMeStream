using StellarMeStream.Resources.Api.Twitch.Data;

namespace StellarMeStream.Resources.Api.Twitch;

internal static class IrcMessageParser
{
    private static readonly Dictionary<string, object> TagsToIgnore = new() { { "client-nonce", null }, { "flags", null } };

    internal static IrcParsedMessage ParseMessage(string message)
    {
        IrcParsedMessage parsedMessage = new();
        int index = 0;
        string rawTagsComponent = null;
        string rawSourceComponent = null;
        string rawParametersComponent = null;
        if (message[index] == '@')
        {
            int endLocalIndex = message.IndexOf(' ');
            rawTagsComponent = message[1..endLocalIndex];
            index = endLocalIndex + 1;
        }
        if (message[index] == ':')
        {
            int endLocalIndex = message.IndexOf(' ', ++index);
            rawSourceComponent = message[index..endLocalIndex];
            index = endLocalIndex + 1;
        }
        int endIndex = message.IndexOf(':', index);
        if (endIndex == -1)
        {
            endIndex = message.Length;
        }
        string rawCommandComponent = message[index..endIndex].Trim();
        if (endIndex != message.Length)
        {
            index = endIndex + 1;
            rawParametersComponent = message[index..];
        }
        parsedMessage.Command = ParseCommand(rawCommandComponent);
        if (parsedMessage.Command is null)
        {
            return null;
        }
        else
        {
            if (rawTagsComponent is not null)
            {
                parsedMessage.Tags = ParseTags(rawTagsComponent);
            }
            parsedMessage.Source = ParseSource(rawSourceComponent);
            parsedMessage.Parameters = rawParametersComponent;
            if (rawParametersComponent is not null && rawParametersComponent[0] == '!')
            {
                parsedMessage.Command = ParseParameters(rawParametersComponent, parsedMessage.Command);
            }
        }
        return parsedMessage;
    }

    private static Dictionary<string, object> ParseTags(string tags)
    {
        Dictionary<string, object> dictParsedTags = [];
        string[] parsedTags = tags.Split(';');
        foreach (string tag in parsedTags)
        {
            string[] parsedTag = tag.Split('=');
            string tagValue = parsedTag[1] == "" ? null : parsedTag[1];
            switch (parsedTag[0])
            {
                case "badges":
                case "badge-info":
                    if (tagValue is not null)
                    {
                        Dictionary<string, string> dict = new();
                        string[] badges = tagValue.Split(',');
                        foreach (string pair in badges)
                        {
                            string[] badgeParts = pair.Split('/');
                            dict[badgeParts[0]] = badgeParts[1];
                        }
                        dictParsedTags[parsedTag[0]] = dict;
                    }
                    else
                    {
                        dictParsedTags[parsedTag[0]] = null;
                    }
                    break;
                case "emotes":
                    if (tagValue is not null)
                    {
                        Dictionary<string, List<IrcEmotePosition>> dictEmotes = new();
                        string[] emotes = tagValue.Split('/');
                        foreach (string emote in emotes)
                        {
                            string[] emoteParts = emote.Split(':');
                            List<IrcEmotePosition> textPositions = new();
                            string[] positions = emoteParts[1].Split(',');
                            foreach (string position in positions)
                            {
                                string[] positionParts = position.Split('-');
                                textPositions.Add(new IrcEmotePosition
                                {
                                    StartPosition = positionParts[0],
                                    EndPosition = positionParts[1]
                                });
                            }
                            dictEmotes[emoteParts[0]] = textPositions;
                        }
                        dictParsedTags[parsedTag[0]] = dictEmotes;
                    }
                    else
                    {
                        dictParsedTags[parsedTag[0]] = null;
                    }
                    break;
                case "emote-sets":
                    string[] emoteSetIds = tagValue.Split(',');
                    dictParsedTags[parsedTag[0]] = emoteSetIds;
                    break;
                default:
                    if (!TagsToIgnore.ContainsKey(parsedTag[0]))
                    {
                        dictParsedTags[parsedTag[0]] = tagValue;
                    }
                    break;
            }
        }
        return dictParsedTags;
    }

    private static IrcParsedCommand ParseCommand(string rawCommandComponent)
    {
        string[] commandParts = rawCommandComponent.Split(' ');
        IrcParsedCommand parsedCommand;
        switch (commandParts[0])
        {
            case "JOIN":
            case "PART":
            case "NOTICE":
            case "CLEARCHAT":
            case "HOSTTARGET":
            case "PRIVMSG":
                parsedCommand = new IrcParsedCommand
                {
                    CommandName = commandParts[0],
                    Channel = commandParts[1]
                };
                break;
            case "PING":
                parsedCommand = new IrcParsedCommand
                {
                    CommandName = commandParts[0]
                };
                break;
            case "CAP":
                parsedCommand = new IrcParsedCommand
                {
                    CommandName = commandParts[0],
                    IsCapRequestEnabled = commandParts[2] == "ACK"
                };
                break;
            case "GLOBALUSERSTATE":
                parsedCommand = new IrcParsedCommand
                {
                    CommandName = commandParts[0]
                };
                break;
            case "USERSTATE":
            case "ROOMSTATE":
                parsedCommand = new IrcParsedCommand
                {
                    CommandName = commandParts[0],
                    Channel = commandParts[1]
                };
                break;
            case "RECONNECT":
                parsedCommand = new IrcParsedCommand
                {
                    CommandName = commandParts[0]
                };
                break;
            case "421":
                return null;
            case "001":
                parsedCommand = new IrcParsedCommand
                {
                    CommandName = commandParts[0],
                    Channel = commandParts[1]
                };
                break;
            case "002":
            case "003":
            case "004":
            case "353":
            case "366":
            case "372":
            case "375":
            case "376":
                return null;
            default:
                return null;
        }
        return parsedCommand;
    }

    private static IrcParsedSource ParseSource(string rawSourceComponent)
    {
        switch (rawSourceComponent)
        {
            case null:
                return null;
            default:
                {
                    string[] sourceParts = rawSourceComponent.Split('!');
                    return new IrcParsedSource
                    {
                        Nick = sourceParts.Length == 2 ? sourceParts[0] : null,
                        Host = sourceParts.Length == 2 ? sourceParts[1] : sourceParts[0]
                    };
                }
        }
    }

    private static IrcParsedCommand ParseParameters(string rawParametersComponent, IrcParsedCommand command)
    {
        int index = 0;
        string commandParts = rawParametersComponent[(index + 1)..].Trim();
        int paramsIndex = commandParts.IndexOf(' ');
        if (paramsIndex == -1)
        {
            command.BotCommand = commandParts;
        }
        else
        {
            command.BotCommand = commandParts[..paramsIndex];
            command.BotCommandParams = commandParts[paramsIndex..].Trim();
            // TODO: remove extra spaces in parameters string
        }
        return command;
    }
}
