using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Markdig;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using MerelyApp.Data;

using Microsoft.Maui.ApplicationModel;

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
            _db = new NotesDatabase(NotesDatabase.GetDefaultDbPath());
        }
        return _db;
    }

    public MainPage()
    {
        InitializeComponent();
        MarkdownEditor.Text = "# Hello Markdown 👋\nType **Markdown** here!";

        MarkdownPreview.Navigating += OnWebViewNavigating;
    }

    // New constructor to open a specific note file
    public MainPage(string filePath) : this()
    {
        _currentFilePath = filePath;

        try
        {
            if (File.Exists(filePath))
            {
                MarkdownEditor.Text = File.ReadAllText(filePath);
            }
            else
            {
                // create empty file if it doesn't exist
                File.WriteAllText(filePath, MarkdownEditor.Text ?? string.Empty);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, $"MainPage ctor load file={filePath}");
            // ignore load errors
        }
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
        string path = _currentFilePath ?? Path.Combine(FileSystem.AppDataDirectory, "note.md");
        try
        {
            await File.WriteAllTextAsync(path, markdown);

            // Save or update database record
            try
            {
                var existing = (await GetDatabase().GetNotesAsync()).FirstOrDefault(n => n.FilePath == path);
                if (existing != null)
                {
                    existing.UpdatedAt = DateTime.UtcNow;
                    await GetDatabase().SaveNoteAsync(existing);
                }
                else
                {
                    var note = new Note { Title = Path.GetFileNameWithoutExtension(path), FilePath = path, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    await GetDatabase().SaveNoteAsync(note);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex, "OnSaveClicked Save DB record");
            }

            await DisplayAlert("Saved", $"Markdown saved to:\n{path}", "OK");
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "OnSaveClicked Write file");
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnFileClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("File", "Cancel", null, "Rename", "Save As", "Open As");
        if (action == "Rename")
        {
            await RenameCurrentFile();
        }
        else if (action == "Save As")
        {
            await SaveAs();
        }
        else if (action == "Open As")
        {
            await OpenAs();
        }
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
        string oldPath = _currentFilePath;
        try
        {
            File.Move(oldPath, newPath);
            _currentFilePath = newPath;

            // Update DB entry file path: find by old path
            try
            {
                var existing = (await GetDatabase().GetNotesAsync()).FirstOrDefault(n => n.FilePath == oldPath);
                if (existing != null)
                {
                    existing.FilePath = newPath;
                    existing.Title = Path.GetFileNameWithoutExtension(newPath);
                    existing.UpdatedAt = DateTime.UtcNow;
                    await GetDatabase().SaveNoteAsync(existing);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex, "RenameCurrentFile Update DB");
            }

            await DisplayAlert("Renamed", $"File renamed to:\n{newPath}", "OK");
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "RenameCurrentFile Move file");
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task SaveAs()
    {
        string defaultName = _currentFilePath != null ? Path.GetFileName(_currentFilePath) : "note.md";
        string newName = await DisplayPromptAsync("Save As", "File name:", initialValue: defaultName);
        if (string.IsNullOrWhiteSpace(newName)) return;

        // Ask where to save
        string locationChoice = await DisplayActionSheet("Save location", "Cancel", null, "App Storage", "Choose folder");
        if (locationChoice == "Cancel") return;

        string targetDir = FileSystem.AppDataDirectory;

        if (locationChoice == "Choose folder")
        {
#if WINDOWS
            var picked = await PickFolderWindowsAsync();
            if (!string.IsNullOrEmpty(picked))
            {
                targetDir = picked;
            }
            else
            {
                // user cancelled folder pick
                await DisplayAlert("Cancelled", "No folder selected. Saving to app storage instead.", "OK");
            }
#else
            await DisplayAlert("Not supported", "Choosing arbitrary folders is only supported on Windows in this build. File will be saved to app storage.", "OK");
#endif
        }

        string newPath = Path.Combine(targetDir, newName);
        try
        {
            await File.WriteAllTextAsync(newPath, MarkdownEditor.Text ?? string.Empty);
            _currentFilePath = newPath;

            // Create DB entry
            try
            {
                var note = new Note { Title = Path.GetFileNameWithoutExtension(newPath), FilePath = newPath, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                await GetDatabase().SaveNoteAsync(note);
            }
            catch (Exception ex)
            {
                AppLogger.Log(ex, "SaveAs Save DB");
            }

            await DisplayAlert("Saved", $"File saved to:\n{newPath}", "OK");
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "SaveAs Write file");
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

#if WINDOWS
    private async Task<string?> PickFolderWindowsAsync()
    {
        try
        {
            // Ensure we run picker code on the UI thread to avoid WinRT runtime exceptions
            string? result = null;
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var picker = new FolderPicker();

                    // Need to initialize with the current window handle
                    var mauiWindow = Microsoft.Maui.Controls.Application.Current?.Windows?.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                    if (mauiWindow == null)
                    {
                        return;
                    }

                    var hwnd = WindowNative.GetWindowHandle(mauiWindow);
                    InitializeWithWindow.Initialize(picker, hwnd);

                    picker.SuggestedStartLocation = PickerLocationId.Desktop;
                    picker.FileTypeFilter.Add("*");

                    var folder = await picker.PickSingleFolderAsync();
                    if (folder != null)
                    {
                        result = folder.Path;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Log(ex, "PickFolderWindowsAsync Picker");
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "PickFolderWindowsAsync Outer");
        }

        return null;
    }
#endif

    private async Task OpenAs()
    {
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Pick a markdown file"
            });

            if (result == null) return;

            // Copy to AppDataDirectory and open
            string dest = Path.Combine(FileSystem.AppDataDirectory, result.FileName);
            using (var stream = await result.OpenReadAsync())
            using (var outStream = File.Create(dest))
            {
                await stream.CopyToAsync(outStream);
            }

            _currentFilePath = dest;
            MarkdownEditor.Text = await File.ReadAllTextAsync(dest);

            // Create DB entry if missing
            try
            {
                var existing = (await GetDatabase().GetNotesAsync()).FirstOrDefault(n => n.FilePath == dest);
                if (existing == null)
                {
                    var note = new Note { Title = Path.GetFileNameWithoutExtension(dest), FilePath = dest, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    await GetDatabase().SaveNoteAsync(note);
                }
            }
            catch { }
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "OpenAs FilePicker");
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }


    private async void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
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

    private async void OnOpenNotesClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new NotesBoardPage());
    }

    private async void OnOpenWeatherClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new WeatherPage());
    }

}
