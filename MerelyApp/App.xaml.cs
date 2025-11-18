using Microsoft.Maui.Controls;

namespace MerelyApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Use a NavigationPage so we can navigate between pages (e.g., NotesBoardPage)
        return new Window(new NavigationPage(new MainPage()));
    }
}
