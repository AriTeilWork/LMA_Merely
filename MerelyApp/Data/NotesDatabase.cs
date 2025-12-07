using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SQLite;

namespace MerelyApp.Data;

public class NotesDatabase
{
    private readonly SQLiteAsyncConnection _database;
    private readonly string _dbPath;
    private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
    private bool _initialized = false;

    public NotesDatabase(string dbPath)
    {
        _dbPath = dbPath;
        _database = new SQLiteAsyncConnection(dbPath);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        
        await _initLock.WaitAsync();
        try
        {
            if (!_initialized)
            {
                await _database.CreateTableAsync<Note>();
                _initialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<Note>> GetNotesAsync()
    {
        await EnsureInitializedAsync();
        return await _database.Table<Note>().OrderByDescending(n => n.UpdatedAt).ToListAsync();
    }

    public async Task<Note> GetNoteAsync(int id)
    {
        await EnsureInitializedAsync();
        return await _database.Table<Note>().Where(n => n.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveNoteAsync(Note note)
    {
        await EnsureInitializedAsync();
        note.UpdatedAt = DateTime.UtcNow;
        if (note.Id != 0)
            return await _database.UpdateAsync(note);
        else
            return await _database.InsertAsync(note);
    }

    public async Task<int> DeleteNoteAsync(Note note)
    {
        await EnsureInitializedAsync();
        return await _database.DeleteAsync(note);
    }

    public static string GetDefaultDbPath()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "notes.db3");
        return path;
    }
}
