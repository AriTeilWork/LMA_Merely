using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using System.Diagnostics;
using MerelyApp;

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
        try
        {
            _database = new SQLiteAsyncConnection(dbPath);
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, $"NotesDatabase ctor dbPath={dbPath}");
            throw;
        }
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
        try
        {
            return await _database.Table<Note>().OrderByDescending(n => n.UpdatedAt).ToListAsync();
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, "GetNotesAsync");
            throw;
        }
    }

    public async Task<Note?> GetNoteAsync(int id)
    {
        await EnsureInitializedAsync();
        try
        {
            return await _database.Table<Note>().Where(n => n.Id == id).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, $"GetNoteAsync id={id}");
            throw;
        }
    }

    public async Task<int> SaveNoteAsync(Note note)
    {
        await EnsureInitializedAsync();
        note.UpdatedAt = DateTime.UtcNow;
        try
        {
            if (note.Id != 0)
                return await _database.UpdateAsync(note);
            else
                return await _database.InsertAsync(note);
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, $"SaveNoteAsync id={note?.Id}");
            throw;
        }
    }

    public async Task<int> DeleteNoteAsync(Note note)
    {
        await EnsureInitializedAsync();
        try
        {
            return await _database.DeleteAsync(note);
        }
        catch (Exception ex)
        {
            AppLogger.Log(ex, $"DeleteNoteAsync id={note?.Id}");
            throw;
        }
    }

    public static string GetDefaultDbPath()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "notes.db3");
        return path;
    }
}
