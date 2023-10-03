using SQLiteNetExtensionsAsync.Extensions;
using StellarMeStream.Resources.Api.TwitchApi.Data;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace StellarMeStream.Resources.Api.TwitchApi;

internal static class TwitchApi
{
	private const string AuthorizeUri = "https://id.twitch.tv/oauth2/authorize";
	private const string BansUri = "https://api.twitch.tv/helix/moderation/bans";
	private const string RedirectUri = "http://localhost:3000/";
	private const string TokenUri = "https://id.twitch.tv/oauth2/token";
	private const string UsersUri = "https://api.twitch.tv/helix/users";
	private const string IrcUri = "wss://irc-ws.chat.twitch.tv:443";

    private static ClientWebSocket IrcClientWebSocket;

	private static readonly List<Timer> TextTimers = new();
	private static readonly List<string> Scopes = [ "analytics:read:extensions", "analytics:read:games", "bits:read", "channel:edit:commercial", "channel:manage:broadcast", "channel:manage:extensions", "channel:manage:guest_star", "channel:manage:moderators", "channel:manage:polls", "channel:manage:predictions", "channel:manage:raids", "channel:manage:redemptions", "channel:manage:schedule", "channel:manage:videos", "channel:manage:vips", "channel:moderate", "channel:read:charity", "channel:read:editors", "channel:read:goals", "channel:read:guest_star", "channel:read:hype_train", "channel:read:polls", "channel:read:predictions", "channel:read:redemptions", "channel:read:stream_key", "channel:read:subscriptions", "channel:read:vips", "chat:edit", "chat:read", "clips:edit", "moderation:read", "moderator:manage:announcements", "moderator:manage:automod", "moderator:manage:automod_settings", "moderator:manage:banned_users", "moderator:manage:blocked_terms", "moderator:manage:chat_messages", "moderator:manage:chat_settings", "moderator:manage:guest_star", "moderator:manage:shield_mode", "moderator:manage:shoutouts", "moderator:read:automod_settings", "moderator:read:blocked_terms", "moderator:read:chat_settings", "moderator:read:chatters", "moderator:read:followers", "moderator:read:guest_star", "moderator:read:shield_mode", "moderator:read:shoutouts", "user:edit", "user:edit:follows", "user:manage:blocked_users", "user:manage:chat_color", "user:manage:whispers", "user:read:blocked_users", "user:read:broadcast", "user:read:email", "user:read:follows", "user:read:subscriptions", "whispers:edit", "whispers:read" ];

	internal static async Task<string> GetAuthorizationCode(string channel)
	{
		HttpListener httpListener = new();
		httpListener.Prefixes.Add(RedirectUri);
		httpListener.Start();
		Task<HttpListenerContext> httpListenerContextTask = httpListener.GetContextAsync();
		string state = Guid.NewGuid().ToString();
		await Browser.OpenAsync(new UriBuilder(AuthorizeUri) { Query = string.Join('&', new Dictionary<string, string>() { { "client_id", TwitchApiSettings.TwitchConnections[channel].Channel.ClientId }, { "redirect_uri", RedirectUri }, { "response_type", "code" }, { "scope", string.Join('+', Scopes.Select(Uri.EscapeDataString)) }, { "state", state } }.Select(keyValuePair => $"{keyValuePair.Key}={keyValuePair.Value}")) }.Uri);
		HttpListenerContext httpListenerContext = await httpListenerContextTask;
		if (state != httpListenerContext.Request.QueryString["state"])
        {
            throw new Exception("State parameter mismatch. Authorization may have been compromised.");
		}
		return httpListenerContext.Request.QueryString["code"];
	}

    internal static async Task<AccessToken> GetAccessToken(string channel) => await (await new HttpClient().PostAsync(TokenUri, new FormUrlEncodedContent(new Dictionary<string, string>() { { "client_id", TwitchApiSettings.TwitchConnections[channel].Channel.ClientId }, { "client_secret", TwitchApiSettings.TwitchConnections[channel].Channel.ClientSecret }, { "code", TwitchApiSettings.TwitchConnections[channel].Channel.Code }, { "grant_type", "authorization_code" }, { "redirect_uri", RedirectUri } }))).EnsureSuccessStatusCode().Content.ReadFromJsonAsync<AccessToken>();

    internal static async Task TimeoutUser(string channel, string targetChannel, string userChannel, int timeoutSeconds, string reason)
	{
		HttpClient httpClient = new();
		httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {TwitchApiSettings.TwitchConnections[channel].Channel.AccessToken.Token}");
		httpClient.DefaultRequestHeaders.Add("Client-Id", TwitchApiSettings.TwitchConnections[channel].Channel.ClientId);
		(await httpClient.PostAsync(BansUri, new StringContent(JsonSerializer.Serialize(new Dictionary<string, object>() { { "broadcaster_id", targetChannel }, { "moderator_id", channel }, { "data", new Dictionary<string, object> { { "user_id", userChannel }, { "duration", timeoutSeconds }, { "reason", reason } } } }), Encoding.UTF8, "application/json"))).EnsureSuccessStatusCode();
	}

	internal static async Task<Users> GetUsers(string channel, string[] usersChannels)
	{
		HttpClient httpClient = new();
		httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {TwitchApiSettings.TwitchConnections[channel].Channel.AccessToken.Token}");
		httpClient.DefaultRequestHeaders.Add("Client-Id", TwitchApiSettings.TwitchConnections[channel].Channel.ClientId);
		return await (await httpClient.GetAsync(new UriBuilder(UsersUri) { Query = string.Join("&", usersChannels.Select(user => $"login={user}")) }.Uri)).EnsureSuccessStatusCode().Content.ReadFromJsonAsync<Users>();
	}

	internal static async Task Connect(string channel)
	{
		await StellarMeStreamDatabase.Initialize();
		Connection twitchConnection = TwitchApiSettings.TwitchConnections[channel];
        IrcClientWebSocket = new ClientWebSocket();
		await IrcClientWebSocket.ConnectAsync(new Uri(IrcUri), CancellationToken.None);
        await SendWebSocketMessage($"PASS oauth:{twitchConnection.Channel.AccessToken.Token}");
        await SendWebSocketMessage("NICK supercherrybanbot");
        await SendWebSocketMessage($"JOIN #{twitchConnection.TargetChannels.First().Key}");
        await SendWebSocketMessage($"CAP REQ :twitch.tv/commands twitch.tv/tags");
        await ReceiveWebSocketMessages(channel);
    }

	private static async Task SendWebSocketMessage(string message)
	{
		if (IrcClientWebSocket.State is not WebSocketState.Open)
		{
			return;
		}
		await IrcClientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"{message}\r\n")), WebSocketMessageType.Text, true, CancellationToken.None);
	}

	private static async Task ReceiveWebSocketMessages(string channel)
	{
		byte[] buffer = new byte[2048];
		while (IrcClientWebSocket.State is WebSocketState.Open)
		{
			try
			{
				WebSocketReceiveResult webSocketReceiveResult;
				MemoryStream memoryStream = new();
				do
				{
					webSocketReceiveResult = await IrcClientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
					memoryStream.Write(buffer, 0, webSocketReceiveResult.Count);
				} while (!webSocketReceiveResult.EndOfMessage);
                if (webSocketReceiveResult.MessageType is WebSocketMessageType.Text)
                {
                    _ = Parallel.ForEachAsync(Encoding.UTF8.GetString(memoryStream.ToArray()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries), async (message, token) =>
                    {
                        IrcParsedMessage ircParsedMessage = IrcMessageParser.ParseMessage(message);
                        if (ircParsedMessage is null)
                        {
                            return;
                        }
                        switch (ircParsedMessage.Command.CommandName)
                        {
                            case "PING":
                                await SendWebSocketMessage($"PONG :{ircParsedMessage.Parameters}");
                                break;
                            case "PRIVMSG":
                                string targetChannel = ircParsedMessage.Command.Channel.Replace("#", string.Empty);
                                string sender = ircParsedMessage.Source.Nick;
                                switch (TwitchChatMessageHandler.IsMessageBad(ircParsedMessage.Parameters))
                                {
                                    case TwitchChatMessageHandler.ChatMessageAction.None:
                                        switch (ircParsedMessage.Command.BotCommand)
                                        {
                                            case "айкью":
                                                if (ircParsedMessage.Command.BotCommandParams is null)
                                                {
                                                    if (Random.Shared.Next(2) == 0)
                                                    {
                                                        await SendReplyChatMessage(targetChannel, ircParsedMessage.Tags["id"].ToString(), $"У тебя {Random.Shared.Next(200)} айкью");
                                                    }
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                        break;
                                    case TwitchChatMessageHandler.ChatMessageAction.Ban:
                                        await TimeoutUser(channel, targetChannel, sender, 600, "ЗАПРЕТКА БАН");
                                        break;
                                    case TwitchChatMessageHandler.ChatMessageAction.Timeout:
                                        await TimeoutUser(channel, targetChannel, sender, 600, "СПАМ МЕШАЕТ");
                                        break;
                                    case TwitchChatMessageHandler.ChatMessageAction.Dino:
                                        await TimeoutUser(channel, targetChannel, sender, 600, "ДИНОЗАВРИК ЛАГАЕТ");
                                        break;
                                    case TwitchChatMessageHandler.ChatMessageAction.Super:
                                        await TimeoutUser(channel, targetChannel, sender, 600, "ТЫ АФИГЕЛ ЧО ЛИ А???");
                                        break;
                                    default:
                                        break;
                                }
                                Message userMessage = new() { Id = ircParsedMessage.Tags["id"].ToString(), Data = ircParsedMessage.Parameters };
                                User user = (await GetUsers(channel, [sender])).UsersData.First();
                                userMessage.User = user;
                                await StellarMeStreamDatabase.CurrentInstance.InsertOrReplaceAsync(user);
                                await StellarMeStreamDatabase.CurrentInstance.InsertWithChildrenAsync(userMessage);
                                break;
                        }
                    });
                }
            }
            catch (Exception)
			{

			}
		}
	}

	internal static void SwitchSpam(bool switcherValue)
	{
        if (switcherValue)
        {
            foreach (Connection connection in TwitchApiSettings.TwitchConnections.Values)
            {
                foreach (string channel in connection.TargetChannels.Keys)
                {
                    TextTimers.Add(new Timer(async state =>
                    {
                        await SendChatMessage(channel, "Boosty (ЗАПИСИ СТРИМОВ) -> boosty.to/kussia1488");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(async state =>
                    {
                        await SendChatMessage(channel, "DonatePay (ДОНАТ) -> new.donatepay.ru/@kussia");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(async state =>
                    {
                        await SendChatMessage(channel, "DonationAlerts (ДОНАТ) -> donationalerts.com/r/kussia");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(async state =>
                    {
                        await SendChatMessage(channel, "Telegram (ПРЕДЛОЖКА В ЗАКРЕПЕ) -> t.me/KussiaOfficial");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(async state =>
                    {
                        await SendChatMessage(channel, "Trovo (УНИКАЛЬНЫЕ СТРИМЫ) -> trovo.live/s/Kussia");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(async state =>
                    {
                        await SendChatMessage(channel, "YouTube (ВТОРОЙ КАНАЛ) -> youtube.com/@kussiastream");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(async state =>
                    {
                        await SendChatMessage(channel, "YouTube (ОСНОВНОЙ КАНАЛ) -> youtube.com/@kussia");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(async state =>
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            await SendChatMessage(channel, "💚 Lolzteam (ЗАХОДИ) -> lolz.link/kussia");
                        }
                    }, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(600)));
                    TextTimers.Add(new Timer(async state =>
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            await SendChatMessage(channel, "❤ УЧАСТВОВАТЬ В РОЗЫГРЫШЕ ДОГОВОРОВ ОТ \"ПризываНет\" -> t.me/bilet_kussia_bot");
                        }
                    }, null, TimeSpan.FromSeconds(400), TimeSpan.FromSeconds(600)));
                    TextTimers.Add(new Timer(async state =>
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            await SendChatMessage(channel, "💜 ИГРАЙ В МАТРЕШКА RP -> matrp.cc/kussia");
                        }
                    }, null, TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(600)));
                }
            }
        }
        else
        {
            foreach (Timer textTimer in TextTimers)
            {
                textTimer.Dispose();
            }
            TextTimers.Clear();
        }
	}

    internal static async Task SendChatMessage(string channel, string message) => await SendWebSocketMessage($"PRIVMSG #{channel} :{message}");

    internal static async Task SendReplyChatMessage(string channel, string messageId, string message) => await SendWebSocketMessage($"@reply-parent-msg-id={messageId} PRIVMSG #{channel} :{message}");
}
