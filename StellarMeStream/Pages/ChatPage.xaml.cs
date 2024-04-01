namespace StellarMeStream.Page;

public partial class ChatPage : ContentPage
{
    internal static ChatPage CurrentInstance { get; set; }

    public ChatPage()
	{
        InitializeComponent();
    }

    private async void SendMessageButtonClicked(object sender, EventArgs e)
    {
        foreach (Resources.Api.Twitch.Data.Connection connection in StellarMeStream.Resources.Api.Twitch.TwitchApiSettings.TwitchConnections.Values)
        {
            foreach (string channel in connection.TargetChannels.Keys)
            {
                await StellarMeStream.Resources.Api.Twitch.TwitchApi.SendChatMessage(channel, MessageToSendEntry.Text);
            }
        }
    }

    private void AddSuperWordButtonClicked(object sender, EventArgs e)
    {
        StellarMeStream.Resources.Api.Twitch.TwitchChatMessageHandler.SuperWords.Add(SuperWordEntry.Text);
    }

    private async void SpamSwitchToggled(object sender, ToggledEventArgs e)
    {
        await StellarMeStream.Resources.Api.Twitch.TwitchApi.SwitchSpamAsync(e.Value);
    }

    private async void TimersButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(TimersPage.CurrentInstance ??= new TimersPage());
    }
}
