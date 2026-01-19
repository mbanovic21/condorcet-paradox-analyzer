using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Platform.Storage;

namespace CondorcetAvalonia.Services;

public static class AvaloniaFileDialog
{
    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;

        return null;
    }

    public static async Task<string?> PickJsonFilePathAsync()
    {
        var win = GetMainWindow();
        if (win is null) return null;

        var files = await win.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open JSON",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("JSON files") { Patterns = new List<string> { "*.json" } },
                new("All files")  { Patterns = new List<string> { "*.*" } },
            }
        });

        return files?.FirstOrDefault() is { } f ? f.Path.LocalPath : null;
    }

    public static async Task ShowErrorAsync(string title, string message)
    {
        var owner = GetMainWindow();
        if (owner is null) return;

        var ok = new Button { Content = "OK", Width = 90, HorizontalAlignment = HorizontalAlignment.Right };
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

        var panel = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(16),
            Children =
            {
                text,
                ok
            }
        };

        var dlg = new Window
        {
            Title = title,
            Width = 520,
            Height = 220,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        ok.Click += (_, _) => dlg.Close();

        await dlg.ShowDialog(owner);
    }
}
