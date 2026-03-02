using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;

namespace Honeycomb.Views;

public partial class MainWindow : Window
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Honeycomb", "window.json");

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RestoreWindowSize();
        Closing += (_, _) => SaveWindowSize();
    }

    private void SaveWindowSize()
    {
        if (WindowState == WindowState.Minimized)
            return;

        var settings = new WindowSettings
        {
            Width = Width,
            Height = Height,
            IsMaximized = WindowState == WindowState.Maximized
        };

        try
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings));
        }
        catch
        {
            // Ignore save failures
        }
    }

    private void RestoreWindowSize()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return;

            var settings = JsonSerializer.Deserialize<WindowSettings>(File.ReadAllText(SettingsPath));
            if (settings is null)
                return;

            Width = settings.Width;
            Height = settings.Height;

            if (settings.IsMaximized)
                WindowState = WindowState.Maximized;
        }
        catch
        {
            // Ignore restore failures
        }
    }

    private sealed class WindowSettings
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMaximized { get; set; }
    }
}
