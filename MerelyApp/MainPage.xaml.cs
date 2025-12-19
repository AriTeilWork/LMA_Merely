using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Markdig;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MerelyApp.Data;
using Microsoft.Maui; 


#if WINDOWS
using WinRT.Interop;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
#endif

namespace MerelyApp;

public partial class MainPage : ContentPage
{
    private string? _currentFilePath;
    private NotesDatabase? _db;

    private NotesDatabase GetDatabase()
    {
        if (_db == null)
        {
            var svc = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services?.GetService(typeof(NotesDatabase)) as NotesDatabase;
            _db = svc ?? new NotesDatabase(NotesDatabase.GetDefaultDbPath());
        }
        return _db;
    }

    public MainPage()
    {
        InitializeComponent();
        MarkdownEditor.Text = "# Hello Markdown 👋\nType **Markdown** here!";
        MarkdownPreview.Navigating += OnWebViewNavigating;
    }

    public MainPage(string filePath) : this()
    {
        _currentFilePath = filePath;

        try
        {
            if (File.Exists(filePath))
                MarkdownEditor.Text = File.ReadAllText(filePath);
            else
                File.WriteAllText(filePath, MarkdownEditor.Text ?? string.Empty);
        }
        catch { /* ignore load errors */ }
    }



    #region Formatting buttons

    private void OnBoldClicked(object sender, EventArgs e) =>
        Utils.EditorFormatting.ApplyBold(MarkdownEditor);

    private void OnItalicClicked(object sender, EventArgs e) =>
        Utils.EditorFormatting.ApplyItalic(MarkdownEditor);

    private void OnHeadingClicked(object sender, EventArgs e) =>
        Utils.EditorFormatting.ApplyHeading(MarkdownEditor);

    private void OnBulletClicked(object sender, EventArgs e) =>
        Utils.EditorFormatting.ApplyBullet(MarkdownEditor);

    private void OnCodeClicked(object sender, EventArgs e) =>
        Utils.EditorFormatting.ApplyInlineCode(MarkdownEditor);

    private void OnH1Clicked(object sender, EventArgs e) =>
        Utils.EditorFormatting.ApplyHeadingLevel(MarkdownEditor, 1);
    private void OnH2Clicked(object sender, EventArgs e) =>
        Utils.EditorFormatting.ApplyHeadingLevel(MarkdownEditor, 2);
    private void OnH3Clicked(object sender, EventArgs e) =>
        Utils.EditorFormatting.ApplyHeadingLevel(MarkdownEditor, 3);
    private void OnH4Clicked(object sender, EventArgs e) =>
        Utils.EditorFormatting.ApplyHeadingLevel(MarkdownEditor, 4);
    private void OnH5Clicked(object sender, EventArgs e) =>
        Utils.EditorFormatting.ApplyHeadingLevel(MarkdownEditor, 5);

    private async void OnLinkClicked(object sender, EventArgs e)
    {
        string defaultText = string.Empty;
        int selStart = Math.Max(0, MarkdownEditor.CursorPosition);
        int selLen = Math.Max(0, MarkdownEditor.SelectionLength);

        if (selLen > 0 && selStart + selLen <= (MarkdownEditor.Text ?? string.Empty).Length)
            defaultText = (MarkdownEditor.Text ?? string.Empty).Substring(selStart, selLen);

        var linkText = await DisplayPromptAsync("Link text", "Text to display (leave empty to use URL):", initialValue: defaultText);
        if (linkText == null) return;

        var linkUrl = await DisplayPromptAsync("Link URL", "Enter URL (https://...):", initialValue: "https://");
        if (linkUrl == null) return;

        linkUrl = linkUrl.Trim();
        if (string.IsNullOrWhiteSpace(linkUrl)) return;

        var inserted = Utils.EditorFormatting.InsertLink(MarkdownEditor, linkText ?? string.Empty, linkUrl);
        if (!inserted)
            await DisplayAlert("Invalid URL", "Please enter a valid http(s) URL.", "OK");

        if (PreviewCheckBox.IsChecked)
            UpdatePreview();
    }

    #endregion

    #region Preview

    private void OnMarkdownTextChanged(object sender, TextChangedEventArgs e)
    {
        if (PreviewCheckBox.IsChecked)
            UpdatePreview();
    }

    private void OnPreviewCheckChanged(object sender, CheckedChangedEventArgs e)
    {
        MarkdownEditor.IsVisible = !e.Value;
        MarkdownPreview.IsVisible = e.Value;

        if (e.Value)
            UpdatePreview();
    }

    private void UpdatePreview()
    {
        string html = Markdown.ToHtml(MarkdownEditor.Text ?? string.Empty);
        MarkdownPreview.Source = new HtmlWebViewSource { Html = html };
    }

    private void OnEditPreviewClicked(object sender, EventArgs e)
    {
        if (PreviewCheckBox.IsChecked)
            PreviewCheckBox.IsChecked = false;

        MarkdownEditor.Focus();
    }

    #endregion

    #region File handling

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string markdown = MarkdownEditor.Text ?? string.Empty;
        string path = _currentFilePath ?? Path.Combine(FileSystem.AppDataDirectory, "note.md");

        try
        {
            await File.WriteAllTextAsync(path, markdown);

            var existing = (await GetDatabase().GetNotesAsync()).FirstOrDefault(n => n.FilePath == path);
            if (existing != null)
            {
                existing.UpdatedAt = DateTime.UtcNow;
                await GetDatabase().SaveNoteAsync(existing);
            }
            else
            {
                var note = new Note
                {
                    Title = Path.GetFileNameWithoutExtension(path),
                    FilePath = path,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await GetDatabase().SaveNoteAsync(note);
            }

            await DisplayAlert("Saved", $"Markdown saved to:\n{path}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnFileClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("File", "Cancel", null, "Rename", "Save As", "Open As");
        if (action == "Rename") await RenameCurrentFile();
        else if (action == "Save As") await SaveAs();
        else if (action == "Open As") await OpenAs();
    }

    private async Task RenameCurrentFile()
    {
        if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
        {
            await DisplayAlert("Rename", "No current file to rename.", "OK");
            return;
        }

        string currentName = Path.GetFileName(_currentFilePath);
        string newName = await DisplayPromptAsync("Rename", "New file name:", initialValue: currentName);
        if (string.IsNullOrWhiteSpace(newName)) return;

        string newPath = Path.Combine(Path.GetDirectoryName(_currentFilePath) ?? FileSystem.AppDataDirectory, newName);

        try
        {
            File.Move(_currentFilePath, newPath);
            string oldPath = _currentFilePath;
            _currentFilePath = newPath;

            var existing = (await GetDatabase().GetNotesAsync()).FirstOrDefault(n => n.FilePath == oldPath);
            if (existing != null)
            {
                existing.FilePath = newPath;
                existing.Title = Path.GetFileNameWithoutExtension(newPath);
                existing.UpdatedAt = DateTime.UtcNow;
                await GetDatabase().SaveNoteAsync(existing);
            }

            await DisplayAlert("Renamed", $"File renamed to:\n{newPath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task SaveAs()
    {
        string defaultName = _currentFilePath != null ? Path.GetFileName(_currentFilePath) : "note.md";
        string newName = await DisplayPromptAsync("Save As", "File name:", initialValue: defaultName);
        if (string.IsNullOrWhiteSpace(newName)) return;

        string locationChoice = await DisplayActionSheet("Save location", "Cancel", null, "App Storage", "Choose folder");
        if (locationChoice == "Cancel") return;

        string targetDir = FileSystem.AppDataDirectory;

        if (locationChoice == "Choose folder")
        {
#if WINDOWS
            var picked = await PickFolderWindowsAsync();
            if (!string.IsNullOrEmpty(picked)) targetDir = picked;
            else await DisplayAlert("Cancelled", "No folder selected. Saving to app storage instead.", "OK");
#else
            await DisplayAlert("Not supported", "Choosing folders is only supported on Windows. File will be saved to app storage.", "OK");
#endif
        }

        string newPath = Path.Combine(targetDir, newName);
        try
        {
            await File.WriteAllTextAsync(newPath, MarkdownEditor.Text ?? string.Empty);
            _currentFilePath = newPath;

            var note = new Note
            {
                Title = Path.GetFileNameWithoutExtension(newPath),
                FilePath = newPath,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await GetDatabase().SaveNoteAsync(note);

            await DisplayAlert("Saved", $"File saved to:\n{newPath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task OpenAs()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Pick a markdown file" });
            if (result == null) return;

            string dest = Path.Combine(FileSystem.AppDataDirectory, result.FileName);
            using (var stream = await result.OpenReadAsync())
            using (var outStream = File.Create(dest))
            {
                await stream.CopyToAsync(outStream);
            }

            _currentFilePath = dest;
            MarkdownEditor.Text = await File.ReadAllTextAsync(dest);

            var existing = (await GetDatabase().GetNotesAsync()).FirstOrDefault(n => n.FilePath == dest);
            if (existing == null)
            {
                var note = new Note
                {
                    Title = Path.GetFileNameWithoutExtension(dest),
                    FilePath = dest,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await GetDatabase().SaveNoteAsync(note);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }
    private async void OnMarkdownHelpClicked(object sender, EventArgs e)
    {
        string helpText = @"
            **Markdown Cheatsheet:**
            - **Bold:** `**text**`
            - *Italic:* `*text*`
            - Headings: `# H1`, `## H2`, `### H3`, etc.
            - Bullet list: `- item`
            - Inline code: `` `code` ``
            - Links: `[text](https://example.com)`
            Use this editor to write Markdown and toggle preview with the checkbox above.
        ";

        await DisplayAlert("Markdown Help", helpText, "Close");
    }


#if WINDOWS
    private async Task<string?> PickFolderWindowsAsync()
    {
        string? result = null;
        await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                var picker = new FolderPicker();
                var mauiWindow = Microsoft.Maui.Controls.Application.Current?.Windows?.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (mauiWindow == null) return;
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(mauiWindow));

                picker.SuggestedStartLocation = PickerLocationId.Desktop;
                picker.FileTypeFilter.Add("*");
                var folder = await picker.PickSingleFolderAsync();
                if (folder != null) result = folder.Path;
            }
            catch { }
        });
        return result;
    }
#endif

    #endregion

    #region Navigation

    private async void OnOpenNotesClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new NotesBoardPage());
    }

    private async void OnOpenWeatherClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new WeatherPage());
    }

    private async void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (Uri.TryCreate(e.Url, UriKind.Absolute, out Uri uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            e.Cancel = true;
            await Launcher.OpenAsync(uri);
        }
    }

    #endregion
}