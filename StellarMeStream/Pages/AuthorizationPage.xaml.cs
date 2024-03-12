using StellarMeStream.Resources.Api.Surreal;
using StellarMeStream.Resources.Api.Twitch;
using StellarMeStream.Resources.Api.Twitch.Data;

namespace StellarMeStream;

public partial class AuthorizationPage : ContentPage
{
    internal static AuthorizationPage CurrentInstance { get; set; }

    public AuthorizationPage()
	{
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ChannelEntry.Text = Preferences.Get(nameof(ChannelEntry), string.Empty);
        TargetChannelEntry.Text = Preferences.Get(nameof(TargetChannelEntry), string.Empty);
        ClientIdEntry.Text = await SecureStorage.GetAsync(nameof(ClientIdEntry));
        ClientSecretEntry.Text = await SecureStorage.GetAsync(nameof(ClientSecretEntry));
    }

    private async void AuthorizeButtonClicked(object sender, EventArgs e)
    {
		try
        {
            string channel = ChannelEntry.Text;
            string targetChannel = TargetChannelEntry.Text;
            TwitchApiSettings.TwitchConnections.TryAdd(channel, new Connection());
            if (!TwitchApiSettings.TwitchConnections.TryGetValue(channel, out Connection twitchConnection))
            {
                throw new Exception("?");
            }
            twitchConnection.Channel.UserData.Login = channel;
            twitchConnection.TargetChannels.TryAdd(targetChannel, new Channel() { UserData = new User() { Login = targetChannel } });
            twitchConnection.Channel.ClientId = ClientIdEntry.Text;
            twitchConnection.Channel.ClientSecret = ClientSecretEntry.Text;
            twitchConnection.Channel.Code = await TwitchApi.GetAuthorizationCode(channel);
            twitchConnection.Channel.AccessToken = await TwitchApi.GetAccessToken(channel);
            Users users = await TwitchApi.GetUsers(channel, [channel, targetChannel]);
            twitchConnection.Channel.UserData = users.UsersData[0];
            twitchConnection.TargetChannels[targetChannel].UserData = users.UsersData[1];
            await TwitchApi.Connect(channel);
        }
        catch (Exception exception)
        {
            await DisplayAlert("Alert", exception.Message, "OK");
        }
    }

    private async void ShowClientIdButtonClicked(object sender, EventArgs e)
    {
        await SurrealApi.Initialize();
        ClientIdEntry.IsPassword = !ClientIdEntry.IsPassword;
        ShowClientIdButton.Text = ShowClientIdButton.Text == "Show" ? "Hide" : "Show";
    }

    private void ShowClientSecretButtonClicked(object sender, EventArgs e)
    {
        ClientSecretEntry.IsPassword = !ClientSecretEntry.IsPassword;
        ShowClientSecretButton.Text = ShowClientSecretButton.Text == "Show" ? "Hide" : "Show";
    }

    private void ChannelEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (ChannelEntry.Text != string.Empty)
        {
            Preferences.Set(nameof(ChannelEntry), ChannelEntry.Text);
        }
        else
        {
            SecureStorage.Remove(nameof(ChannelEntry));
        }
    }

    private void TargetChannelEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (TargetChannelEntry.Text != string.Empty)
        {
            Preferences.Set(nameof(TargetChannelEntry), TargetChannelEntry.Text);
        }
        else
        {
            SecureStorage.Remove(nameof(TargetChannelEntry));
        }
    }

    private async void ClientIdEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (ClientIdEntry.Text != string.Empty)
        {
            await SecureStorage.SetAsync(nameof(ClientIdEntry), ClientIdEntry.Text);
        }
        else
        {
            SecureStorage.Remove(nameof(ClientIdEntry));
        }
    }

    private async void ClientSecretEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (ClientSecretEntry.Text != string.Empty)
        {
            await SecureStorage.SetAsync(nameof(ClientSecretEntry), ClientSecretEntry.Text);
        }
        else
        {
            SecureStorage.Remove(nameof(ClientSecretEntry));
        }
    }
}
