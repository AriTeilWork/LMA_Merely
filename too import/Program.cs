using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace MauiNotes;

public static class Program
{
    public static MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    static void Main(string[] args)
    {
        var app = CreateMauiApp();
    }
}
