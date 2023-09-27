using StellarMeStream.Resources.Api.TwitchApi;
using StellarMeStream.Resources.Api.TwitchApi.Data;

namespace StellarMeStream;

public partial class ChatPage : ContentPage
{
    internal static ChatPage CurrentInstance { get; set; }

    public ChatPage()
	{
		InitializeComponent();
    }

    private void SendMessageButtonClicked(object sender, EventArgs e)
    {
        foreach (Connection connection in TwitchApiSettings.TwitchConnections.Values)
        {
            foreach (string channel in connection.TargetChannels.Keys)
            {
                TwitchApi.SendChatMessage(channel, MessageToSendEntry.Text);
            }
        }
    }

    private void AddSuperWordButtonClicked(object sender, EventArgs e)
    {
        TwitchChatMessageHandler.SuperWords.Add(SuperWordEntry.Text);
    }

    private void SpamSwitchToggled(object sender, ToggledEventArgs e)
    {
        TwitchApi.SwitchSpam(e.Value);
    }
}
