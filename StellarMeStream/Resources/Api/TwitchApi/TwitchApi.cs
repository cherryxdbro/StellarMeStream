using SQLite;
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

    private static SQLiteAsyncConnection StellarMeStreamDatabase;
    private static ClientWebSocket IrcClientWebSocket;

	private static readonly List<Timer> TextTimers = new();
	private static readonly List<string> Scopes = [ "analytics:read:extensions", "analytics:read:games", "bits:read", "channel:edit:commercial", "channel:manage:broadcast", "channel:manage:extensions", "channel:manage:guest_star", "channel:manage:moderators", "channel:manage:polls", "channel:manage:predictions", "channel:manage:raids", "channel:manage:redemptions", "channel:manage:schedule", "channel:manage:videos", "channel:manage:vips", "channel:moderate", "channel:read:charity", "channel:read:editors", "channel:read:goals", "channel:read:guest_star", "channel:read:hype_train", "channel:read:polls", "channel:read:predictions", "channel:read:redemptions", "channel:read:stream_key", "channel:read:subscriptions", "channel:read:vips", "chat:edit", "chat:read", "clips:edit", "moderation:read", "moderator:manage:announcements", "moderator:manage:automod", "moderator:manage:automod_settings", "moderator:manage:banned_users", "moderator:manage:blocked_terms", "moderator:manage:chat_messages", "moderator:manage:chat_settings", "moderator:manage:guest_star", "moderator:manage:shield_mode", "moderator:manage:shoutouts", "moderator:read:automod_settings", "moderator:read:blocked_terms", "moderator:read:chat_settings", "moderator:read:chatters", "moderator:read:followers", "moderator:read:guest_star", "moderator:read:shield_mode", "moderator:read:shoutouts", "user:edit", "user:edit:follows", "user:manage:blocked_users", "user:manage:chat_color", "user:manage:whispers", "user:read:blocked_users", "user:read:broadcast", "user:read:email", "user:read:follows", "user:read:subscriptions", "whispers:edit", "whispers:read" ];

    internal static void Initialize()
	{
        StellarMeStreamDatabase = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
		StellarMeStreamDatabase.CreateTablesAsync<User, Message>().Wait();
    }

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

    internal static AccessToken GetAccessToken(string channel) => new HttpClient().PostAsync(TokenUri, new FormUrlEncodedContent(new Dictionary<string, string>() { { "client_id", TwitchApiSettings.TwitchConnections[channel].Channel.ClientId }, { "client_secret", TwitchApiSettings.TwitchConnections[channel].Channel.ClientSecret }, { "code", TwitchApiSettings.TwitchConnections[channel].Channel.Code }, { "grant_type", "authorization_code" }, { "redirect_uri", RedirectUri } })).Result.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<AccessToken>().Result;

    internal static void TimeoutUser(string channel, string targetChannel, string userChannel, int timeoutSeconds, string reason)
	{
		Users twitchUsers = GetUsers(channel, [channel, targetChannel, userChannel]);
		string moderatorId = twitchUsers.UsersData.First(twitchUser => twitchUser.Login == channel).Id;
		string broadcasterId = twitchUsers.UsersData.First(twitchUser => twitchUser.Login == targetChannel).Id;
		string userId = twitchUsers.UsersData.First(twitchUser => twitchUser.Login == userChannel).Id;
		HttpClient httpClient = new();
		httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {TwitchApiSettings.TwitchConnections[channel].Channel.AccessToken.Token}");
		httpClient.DefaultRequestHeaders.Add("Client-Id", TwitchApiSettings.TwitchConnections[channel].Channel.ClientId);
		httpClient.PostAsync(BansUri, new StringContent(JsonSerializer.Serialize(new Dictionary<string, object>() { { "broadcaster_id", broadcasterId }, { "moderator_id", moderatorId }, { "data", new Dictionary<string, object> { { "user_id", userId }, { "duration", timeoutSeconds }, { "reason", reason } } } }), Encoding.UTF8, "application/json")).Result.EnsureSuccessStatusCode();
	}

	internal static Users GetUsers(string channel, string[] usersChannels)
	{
		HttpClient httpClient = new();
		httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {TwitchApiSettings.TwitchConnections[channel].Channel.AccessToken.Token}");
		httpClient.DefaultRequestHeaders.Add("Client-Id", TwitchApiSettings.TwitchConnections[channel].Channel.ClientId);
		return httpClient.GetAsync(new UriBuilder(UsersUri) { Query = string.Join("&", usersChannels.Select(user => $"login={user}")) }.Uri).Result.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<Users>().Result;
	}

	internal static void Connect(object channelObject)
	{
		if (channelObject is string channel)
		{
			Connection twitchConnection = TwitchApiSettings.TwitchConnections[channel];
            IrcClientWebSocket = new ClientWebSocket();
			IrcClientWebSocket.ConnectAsync(new Uri(IrcUri), CancellationToken.None).Wait();
            SendWebSocketMessage($"PASS oauth:{twitchConnection.Channel.AccessToken.Token}");
            SendWebSocketMessage("NICK supercherrybanbot");
            SendWebSocketMessage($"JOIN #{twitchConnection.TargetChannels.First().Key}");
            SendWebSocketMessage($"CAP REQ :twitch.tv/commands twitch.tv/tags");
            ReceiveWebSocketMessages(channel);
        }
    }

	private static void SendWebSocketMessage(string message)
	{
		if (IrcClientWebSocket.State is not WebSocketState.Open)
		{
			return;
		}
		IrcClientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes($"{message}\r\n")), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
	}

	private static void ReceiveWebSocketMessages(string channel)
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
					webSocketReceiveResult = IrcClientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;
					memoryStream.Write(buffer, 0, webSocketReceiveResult.Count);
				} while (!webSocketReceiveResult.EndOfMessage);
				if (webSocketReceiveResult.MessageType is WebSocketMessageType.Text)
				{
					foreach (string message in Encoding.UTF8.GetString(memoryStream.ToArray()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
					{
						new Thread(() =>
						{
							IrcParsedMessage ircParsedMessage = IrcMessageParser.ParseMessage(message);
							if (ircParsedMessage is null)
							{
								return;
							}
							switch (ircParsedMessage.Command.CommandName)
							{
								case "PING":
									SendWebSocketMessage($"PONG :{ircParsedMessage.Parameters}");
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
															SendReplyChatMessage(targetChannel, ircParsedMessage.Tags["id"].ToString(), $"У тебя {Random.Shared.Next(200)} айкью");
														}
													}
													break;
												default:
													break;
											}
											break;
										case TwitchChatMessageHandler.ChatMessageAction.Ban:
											TimeoutUser(channel, targetChannel, sender, 600, "ЗАПРЕТКА БАН");
											break;
										case TwitchChatMessageHandler.ChatMessageAction.Timeout:
											TimeoutUser(channel, targetChannel, sender, 600, "СПАМ МЕШАЕТ");
											break;
										case TwitchChatMessageHandler.ChatMessageAction.Dino:
											TimeoutUser(channel, targetChannel, sender, 600, "ДИНОЗАВРИК ЛАГАЕТ");
											break;
										case TwitchChatMessageHandler.ChatMessageAction.Super:
											TimeoutUser(channel, targetChannel, sender, 600, "ТЫ АФИГЕЛ ЧО ЛИ А???");
											break;
										default:
											break;
									}
									break;
							}
						}).Start();
					}
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
                    TextTimers.Add(new Timer(state =>
                    {
                        SendChatMessage(channel, "Boosty (ЗАПИСИ СТРИМОВ) -> boosty.to/kussia1488");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(state =>
                    {
                        SendChatMessage(channel, "DonatePay (ДОНАТ) -> new.donatepay.ru/@kussia");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(state =>
                    {
                        SendChatMessage(channel, "DonationAlerts (ДОНАТ) -> donationalerts.com/r/kussia");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(state =>
                    {
                        SendChatMessage(channel, "Telegram (ПРЕДЛОЖКА В ЗАКРЕПЕ) -> t.me/KussiaOfficial");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(state =>
                    {
                        SendChatMessage(channel, "Trovo (УНИКАЛЬНЫЕ СТРИМЫ) -> trovo.live/s/Kussia");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(state =>
                    {
                        SendChatMessage(channel, "YouTube (ВТОРОЙ КАНАЛ) -> youtube.com/@kussiastream");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(state =>
                    {
                        SendChatMessage(channel, "YouTube (ОСНОВНОЙ КАНАЛ) -> youtube.com/@kussia");
                    }, null, TimeSpan.FromSeconds(Random.Shared.Next(200)), TimeSpan.FromSeconds(Random.Shared.Next(500, 600))));
                    TextTimers.Add(new Timer(state =>
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            SendChatMessage(channel, "Lolzteam (ЗАХОДИ) -> lolz.link/kussia");
                        }
                    }, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(600)));
                    TextTimers.Add(new Timer(state =>
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            SendChatMessage(channel, "❤️ УЧАСТВОВАТЬ В РОЗЫГРЫШЕ ДОГОВОРОВ ОТ \"ПризываНет\" - t.me/bilet_kussia_bot");
                        }
                    }, null, TimeSpan.FromSeconds(300), TimeSpan.FromSeconds(600)));
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

    internal static void SendChatMessage(string channel, string message) => SendWebSocketMessage($"PRIVMSG #{channel} :{message}");

    internal static void SendReplyChatMessage(string channel, string messageId, string message) => SendWebSocketMessage($"@reply-parent-msg-id={messageId} PRIVMSG #{channel} :{message}");
}
