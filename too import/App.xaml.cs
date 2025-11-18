using Microsoft.Maui.Controls;

namespace MauiNotes;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new NotesBoardPage());
    }
}
