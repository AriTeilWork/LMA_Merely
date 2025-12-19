using System;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using MerelyApp.Data;

namespace MerelyApp;

public partial class NotesBoardPage : ContentPage
{
    private ObservableCollection<Note> _notes = new ObservableCollection<Note>();
    private NotesDatabase? _db;

    private NotesDatabase GetDatabase()
    {
        if (_db == null)
        {
            var svc = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services?.GetService(typeof(NotesDatabase)) as NotesDatabase;
            if (svc != null)
                _db = svc;
            else
                _db = new NotesDatabase(NotesDatabase.GetDefaultDbPath());
        }
        return _db;
    }

    public NotesBoardPage()
    {
        InitializeComponent();
        NotesList.ItemsSource = _notes;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadNotesFromDb();
    }

    private async void LoadNotesFromDb()
    {
        try
        {
            var notes = await GetDatabase().GetNotesAsync();
            _notes.Clear();
            foreach (var n in notes)
            {
                _notes.Add(n);
            }

            if (_notes.Count == 0)
            {
                // add a default sample note
                var file = Path.Combine(FileSystem.AppDataDirectory, $"note_{Guid.NewGuid()}.md");
                await File.WriteAllTextAsync(file, "# Welcome\nCreate notes using the New Note button.");
                var note = new Note { Title = "Welcome", FilePath = file, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                await GetDatabase().SaveNoteAsync(note);
                _notes.Add(note);
            }
        }
        catch
        {
            // fallback to scanning AppDataDirectory
            try
            {
                var dir = FileSystem.AppDataDirectory;
                var files = Directory.GetFiles(dir, "*.md");
                foreach (var f in files)
                {
                    var note = new Note { Title = Path.GetFileNameWithoutExtension(f), FilePath = f, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    _notes.Add(note);
                }
            }
            catch { }
        }
    }

    private async void OnAddNewNoteClicked(object sender, EventArgs e)
    {
        string file = Path.Combine(FileSystem.AppDataDirectory, $"note_{Guid.NewGuid()}.md");
        await File.WriteAllTextAsync(file, "# New note\nWrite content here...");

        var note = new Note { Title = Path.GetFileNameWithoutExtension(file), FilePath = file, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        await GetDatabase().SaveNoteAsync(note);
        _notes.Insert(0, note);

        // Open the new note in the Markdown editor
        await Navigation.PushAsync(new MainPage(file));
    }

    private async void OnOpenNoteClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Note note)
        {
            if (File.Exists(note.FilePath))
            {
                await Navigation.PushAsync(new MainPage(note.FilePath));
            }
            else
            {
                await DisplayAlert("Error", "Note file not found.", "OK");
            }
        }
    }

    private async void OnRenameNoteClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Note note)
        {
            string newName = await DisplayPromptAsync("Rename", "New file name:", initialValue: Path.GetFileName(note.FilePath));
            if (string.IsNullOrWhiteSpace(newName)) return;

            // sanitize and ensure safe length
            newName = MerelyApp.Utils.FileNameUtils.EnsureSafeFileName(Path.GetDirectoryName(note.FilePath) ?? FileSystem.AppDataDirectory, newName);
            string newPath = Path.Combine(Path.GetDirectoryName(note.FilePath) ?? FileSystem.AppDataDirectory, newName);
            try
            {
                File.Move(note.FilePath, newPath);
                note.FilePath = newPath;
                note.Title = Path.GetFileNameWithoutExtension(newPath);
                note.UpdatedAt = DateTime.UtcNow;
                await GetDatabase().SaveNoteAsync(note);

                // refresh UI
                var idx = _notes.IndexOf(note);
                if (idx >= 0)
                {
                    _notes[idx] = note;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }

    private async void OnDeleteNoteClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is Note note)
        {
            bool ok = await DisplayAlert("Delete", $"Delete '{note.Title}'?", "Yes", "No");
            if (!ok) return;

            try
            {
                if (File.Exists(note.FilePath)) File.Delete(note.FilePath);
                await GetDatabase().DeleteNoteAsync(note);
                _notes.Remove(note);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
