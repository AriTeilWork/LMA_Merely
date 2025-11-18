using System;
using SQLite;

namespace MerelyApp.Data;

public class Note
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; }

    public string FilePath { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Note()
    {
        Title = string.Empty;
        FilePath = string.Empty;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}