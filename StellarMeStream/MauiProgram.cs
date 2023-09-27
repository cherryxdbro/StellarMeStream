#if DEBUG
using Microsoft.Extensions.Logging;
#endif

using StellarMeStream.Resources.Api.TwitchApi;

namespace StellarMeStream;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        TwitchApi.Initialize();
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
#if DEBUG
		builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
