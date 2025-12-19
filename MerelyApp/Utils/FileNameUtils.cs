using System;
using System.IO;

namespace MerelyApp.Utils;

public static class FileNameUtils
{
    // Basic sanitization: replace invalid filename chars and ensure .md extension
    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "note.md";
        name = name.Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c.ToString(), "_");
        }
        // collapse multiple dots at start
        while (name.StartsWith(".")) name = name.TrimStart('.');
        if (string.IsNullOrWhiteSpace(name)) return "note.md";
        if (!name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            name = name + ".md";
        return name;
    }

    // Ensure resulting full path length is reasonable; truncate base name if necessary
    public static string EnsureSafeFileName(string directory, string name, int maxPath = 260)
    {
        name = SanitizeFileName(name);
        if (string.IsNullOrEmpty(directory)) directory = FileSystem.AppDataDirectory;
        string full = Path.Combine(directory, name);
        try
        {
            full = Path.GetFullPath(full);
        }
        catch
        {
            // if GetFullPath fails, fall back to combine result
        }

        if (full.Length <= maxPath) return name;

        var ext = Path.GetExtension(name);
        var baseName = Path.GetFileNameWithoutExtension(name);
        int avail = Math.Max(1, maxPath - (directory.Length + Path.DirectorySeparatorChar.ToString().Length + ext.Length));
        if (avail < 1) avail = 1;
        if (baseName.Length > avail)
        {
            baseName = baseName.Substring(0, avail);
        }
        var result = baseName + ext;
        return result;
    }
}
