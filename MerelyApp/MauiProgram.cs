using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using SQLitePCL;

namespace MerelyApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Ensure SQLite native libraries are initialized on Android
        try
        {
            // Use Batteries_V2 when available (bundle_green) for better native support
            try
            {
                SQLitePCL.Batteries_V2.Init();
            }
            catch
            {
                SQLitePCL.Batteries.Init();
            }
        }
        catch
        {
            // ignore if initialization not necessary
        }

        return builder.Build();
    }
}
