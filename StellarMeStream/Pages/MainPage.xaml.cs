namespace StellarMeStream.Page;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        StellarMeStream.Resources.Api.Surreal.SurrealApi.Initialize();
    }

    private async void AuthorizationButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(AuthorizationPage.CurrentInstance ??= new AuthorizationPage());
    }

    private async void ChatButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(ChatPage.CurrentInstance ??= new ChatPage());
    }

    private async void PollButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(PollPage.CurrentInstance ??= new PollPage());
    }

    private async void PredictionButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(PredictionPage.CurrentInstance ??= new PredictionPage());
    }

    private async void SettingsButtonClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(SettingsPage.CurrentInstance ??= new SettingsPage());
    }
}
