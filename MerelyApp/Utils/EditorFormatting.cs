using Microsoft.Maui.Controls;
using System;

namespace MerelyApp.Utils;

public static class EditorFormatting
{
    private static string Normalize(string? s) =>
        (s ?? string.Empty).Replace("\r\n", "\n");

    #region Inline formatting

    public static void WrapSelection(Editor editor, string prefix, string suffix)
    {
        if (editor == null) return;

        var text = Normalize(editor.Text);
        int selStart = Math.Clamp(editor.CursorPosition, 0, text.Length);
        int selLen = Math.Clamp(editor.SelectionLength, 0, text.Length - selStart);

        var before = text.Substring(0, selStart);
        var selected = text.Substring(selStart, selLen);
        var after = text.Substring(selStart + selLen);

        var replaced = prefix + selected + suffix;
        editor.Text = before + replaced + after;

        editor.CursorPosition = before.Length + replaced.Length;
        editor.SelectionLength = 0;
        editor.Focus();
    }

    public static void ApplyBold(Editor editor) =>
        WrapSelection(editor, "**", "**");

    public static void ApplyItalic(Editor editor) =>
        WrapSelection(editor, "*", "*");

    public static void ApplyInlineCode(Editor editor) =>
        WrapSelection(editor, "`", "`");

    #endregion

    #region Headings

    public static void ApplyHeading(Editor editor) =>
        ApplyHeadingLevel(editor, 1);

    public static void ApplyHeadingLevel(Editor editor, int level)
    {
        if (editor == null) return;
        level = Math.Clamp(level, 1, 6);

        var text = Normalize(editor.Text);
        int cursor = Math.Clamp(editor.CursorPosition, 0, text.Length);
        int selLen = Math.Max(0, editor.SelectionLength);

        int start = text.LastIndexOf('\n', Math.Max(0, cursor - 1));
        start = start == -1 ? 0 : start + 1;

        int end = selLen == 0
            ? text.IndexOf('\n', cursor)
            : text.IndexOf('\n', Math.Min(text.Length, cursor + selLen));

        if (end == -1) end = text.Length;

        var before = text[..start];
        var middle = text[start..end];
        var after = text[end..];

        var lines = middle.Split('\n');
        string prefix = new string('#', level) + " ";

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            int ws = line.Length - line.TrimStart().Length;
            string leading = line[..ws];
            string content = line[ws..];

            // toggle
            if (content.StartsWith(prefix))
            {
                lines[i] = leading + content[prefix.Length..];
            }
            else
            {
                content = content.TrimStart('#').TrimStart();
                lines[i] = leading + prefix + content;
            }
        }

        var newMiddle = string.Join("\n", lines);
        editor.Text = before + newMiddle + after;
        editor.CursorPosition = before.Length + newMiddle.Length;
        editor.SelectionLength = 0;
        editor.Focus();
    }

    #endregion

    #region Lists

    public static void ApplyBullet(Editor editor)
    {
        if (editor == null) return;

        var text = Normalize(editor.Text);
        int cursor = Math.Clamp(editor.CursorPosition, 0, text.Length);
        int selLen = Math.Max(0, editor.SelectionLength);

        int start = text.LastIndexOf('\n', Math.Max(0, cursor - 1));
        start = start == -1 ? 0 : start + 1;

        int end = selLen == 0
            ? text.IndexOf('\n', cursor)
            : text.IndexOf('\n', Math.Min(text.Length, cursor + selLen));

        if (end == -1) end = text.Length;

        var before = text[..start];
        var middle = text[start..end];
        var after = text[end..];

        var lines = middle.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            int ws = line.Length - line.TrimStart().Length;
            string leading = line[..ws];
            string content = line[ws..];

            if (content.StartsWith("- ") ||
                content.StartsWith("* ") ||
                content.StartsWith("+ "))
            {
                // toggle off
                lines[i] = leading + content[2..];
            }
            else
            {
                lines[i] = leading + "- " + content;
            }
        }

        var newMiddle = string.Join("\n", lines);
        editor.Text = before + newMiddle + after;
        editor.CursorPosition = before.Length + newMiddle.Length;
        editor.SelectionLength = 0;
        editor.Focus();
    }

    #endregion

    #region Links

    public static bool InsertLink(Editor editor, string text, string url)
    {
        if (editor == null) return false;
        if (string.IsNullOrWhiteSpace(url)) return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var tryUrl = "https://" + url;
                if (!Uri.TryCreate(tryUrl, UriKind.Absolute, out uri))
                    return false;

                url = tryUrl;
            }
            else return false;
        }

        var src = Normalize(editor.Text);
        int selStart = Math.Clamp(editor.CursorPosition, 0, src.Length);
        int selLen = Math.Clamp(editor.SelectionLength, 0, src.Length - selStart);

        var before = src.Substring(0, selStart);
        var selected = src.Substring(selStart, selLen);
        var after = src.Substring(selStart + selLen);

        string linkText = string.IsNullOrWhiteSpace(selected)
            ? (string.IsNullOrWhiteSpace(text) ? url : text)
            : selected;

        var inserted = $"[{linkText}]({url})";
        editor.Text = before + inserted + after;
        editor.CursorPosition = before.Length + inserted.Length;
        editor.SelectionLength = 0;
        editor.Focus();

        return true;
    }

    #endregion
}