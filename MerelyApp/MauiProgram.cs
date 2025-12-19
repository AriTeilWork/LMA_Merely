using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using SQLitePCL;
using System;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;

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

        // Global exception handlers
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                    AppLogger.Log(ex, "UnhandledException");
                else
                    AppLogger.Log($"Unhandled exception object: {e.ExceptionObject}");
            }
            catch { }
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            try
            {
                AppLogger.Log(e.Exception, "UnobservedTaskException");
                e.SetObserved();
            }
            catch { }
        };

        // Initialize SQLite native provider early for all platforms.
        try
        {
            // Initialize batteries (will load provider assembly based on referenced bundle)
            SQLitePCL.Batteries_V2.Init();

            // Some provider types are added only by platform-specific provider packages
            // and may not be available at compile time. Try to locate a provider type
            // at runtime (winsqlite3, e_sqlite3, or sqlite3) and set it via reflection.
            try
            {
                Type? providerType = null;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    providerType = asm.GetType("SQLitePCL.SQLite3Provider_winsqlite3")
                                ?? asm.GetType("SQLitePCL.SQLite3Provider_e_sqlite3")
                                ?? asm.GetType("SQLitePCL.SQLite3Provider_sqlite3");
                    if (providerType != null) break;
                }

                if (providerType != null)
                {
                    var provider = Activator.CreateInstance(providerType);
                    if (provider != null)
                    {
                        try
                        {
                            // Use dynamic to avoid needing compile-time type references
                            SQLitePCL.raw.SetProvider((dynamic)provider);
                        }
                        catch
                        {
                            // ignore if provider already set or fails
                        }
                    }
                }
            }
            catch
            {
                // ignore provider reflection failures
            }
        }
        catch
        {
            try
            {
                SQLitePCL.Batteries.Init();
            }
            catch
            {
                // ignore fallback failures
            }
        }

        // Register app services in DI
        builder.Services.AddSingleton<MerelyApp.Data.NotesDatabase>(sp => new MerelyApp.Data.NotesDatabase(MerelyApp.Data.NotesDatabase.GetDefaultDbPath()));
        builder.Services.AddSingleton<MerelyApp.Services.OpenMeteoWeatherService>();
        builder.Services.AddSingleton<MerelyApp.Services.WeatherService>();

        return builder.Build();
    }
}

