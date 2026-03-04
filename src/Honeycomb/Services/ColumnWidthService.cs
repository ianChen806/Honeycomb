using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Honeycomb.Services;

public static class ColumnWidthService
{
    private static readonly string DefaultFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Honeycomb", "column-widths.json");

    public static Dictionary<string, double> Load(int categoryId, string? filePath = null)
    {
        var path = filePath ?? DefaultFilePath;
        try
        {
            if (!File.Exists(path))
                return new Dictionary<string, double>();

            var json = File.ReadAllText(path);
            var all = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, double>>>(json);
            var key = categoryId.ToString();

            if (all is not null && all.TryGetValue(key, out var widths))
                return widths;
        }
        catch
        {
            // Corrupted file — fall back to defaults
        }

        return new Dictionary<string, double>();
    }

    public static void Save(int categoryId, Dictionary<string, double> widths, string? filePath = null)
    {
        var path = filePath ?? DefaultFilePath;
        try
        {
            Dictionary<string, Dictionary<string, double>> all;

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                all = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, double>>>(json)
                      ?? new Dictionary<string, Dictionary<string, double>>();
            }
            else
            {
                all = new Dictionary<string, Dictionary<string, double>>();
            }

            all[categoryId.ToString()] = widths;

            var dir = Path.GetDirectoryName(path);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, JsonSerializer.Serialize(all));
        }
        catch
        {
            // Ignore save failures
        }
    }
}
