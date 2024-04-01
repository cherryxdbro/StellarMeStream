namespace StellarMeStream.Page;

public partial class TimersPage : ContentPage
{
    internal static TimersPage CurrentInstance { get; set; }

    public TimersPage()
	{
        InitializeComponent();
    }

    private async void CreateTimerButtonClicked(object sender, EventArgs e)
    {
        await StellarMeStream.Resources.Api.Surreal.SurrealApi.SurrealDbClient.RawQuery($"create timers:{IdFieldEntry.Text} set count = {CountFieldEntry.Text}, enabled = {EnabledFieldEntry.Text}, message = {MessageFieldEntry.Text}, offset = {OffsetFieldEntry.Text}, period = {PeriodFieldEntry.Text};");
    }

    private async void UpdateTimerButtonClicked(object sender, EventArgs e)
    {
        await StellarMeStream.Resources.Api.Surreal.SurrealApi.SurrealDbClient.RawQuery($"update timers:{IdFieldEntry.Text} set count = {CountFieldEntry.Text}, enabled = {EnabledFieldEntry.Text}, message = {MessageFieldEntry.Text}, offset = {OffsetFieldEntry.Text}, period = {PeriodFieldEntry.Text};");
    }
}
