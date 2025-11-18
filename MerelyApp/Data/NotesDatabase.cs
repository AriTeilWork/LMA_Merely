using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;

namespace MerelyApp.Data;

public class NotesDatabase
{
    private readonly SQLiteAsyncConnection _database;

    public NotesDatabase(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        _database.CreateTableAsync<Note>().Wait();
    }

    public Task<List<Note>> GetNotesAsync()
    {
        return _database.Table<Note>().OrderByDescending(n => n.UpdatedAt).ToListAsync();
    }

    public Task<Note> GetNoteAsync(int id)
    {
        return _database.Table<Note>().Where(n => n.Id == id).FirstOrDefaultAsync();
    }

    public Task<int> SaveNoteAsync(Note note)
    {
        note.UpdatedAt = DateTime.UtcNow;
        if (note.Id != 0)
            return _database.UpdateAsync(note);
        else
            return _database.InsertAsync(note);
    }

    public Task<int> DeleteNoteAsync(Note note)
    {
        return _database.DeleteAsync(note);
    }

    public static string GetDefaultDbPath()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "notes.db3");
        return path;
    }
}
