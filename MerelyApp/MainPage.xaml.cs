using Microsoft.Maui.Controls;
using Markdig;

namespace MerelyApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        MarkdownEditor.Text = "# Hello Markdown 👋\nType **Markdown** here!";

        MarkdownPreview.Navigating += OnWebViewNavigating;
    }

    private void OnMarkdownTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!PreviewCheckBox.IsChecked) return;

        string html = Markdown.ToHtml(e.NewTextValue ?? string.Empty);
        MarkdownPreview.Source = new HtmlWebViewSource { Html = html };
    }

    private void OnPreviewCheckChanged(object sender, CheckedChangedEventArgs e)
    {
        MarkdownEditor.IsVisible = !e.Value;
        MarkdownPreview.IsVisible = e.Value;

        if (e.Value)
        {
            string html = Markdown.ToHtml(MarkdownEditor.Text ?? string.Empty);
            MarkdownPreview.Source = new HtmlWebViewSource { Html = html };
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string markdown = MarkdownEditor.Text ?? string.Empty;
        string path = Path.Combine(FileSystem.AppDataDirectory, "note.md");
        await File.WriteAllTextAsync(path, markdown);
        await DisplayAlert("Saved", $"Markdown saved to:\n{path}", "OK");
    }



    private async void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (Uri.TryCreate(e.Url, UriKind.Absolute, out Uri uri))
        {
            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            {
                e.Cancel = true;
                await Launcher.OpenAsync(uri);
            }
        }
    }



}
