using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Honeycomb.Services;
using Xunit;

namespace Honeycomb.Tests.Services;

public class ColumnWidthServiceTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _filePath;

    public ColumnWidthServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"honeycomb-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _filePath = Path.Combine(_testDir, "column-widths.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }

    [Fact]
    public void Load_ReturnsEmpty_WhenFileDoesNotExist()
    {
        var widths = ColumnWidthService.Load(1, Path.Combine(_testDir, "nonexistent.json"));

        Assert.Empty(widths);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var input = new Dictionary<string, double>
        {
            ["商品名稱"] = 200,
            ["單價"] = 120
        };

        ColumnWidthService.Save(1, input, _filePath);
        var result = ColumnWidthService.Load(1, _filePath);

        Assert.Equal(200, result["商品名稱"]);
        Assert.Equal(120, result["單價"]);
    }

    [Fact]
    public void Save_DifferentCategories_Independent()
    {
        var widths1 = new Dictionary<string, double> { ["商品名稱"] = 200 };
        var widths2 = new Dictionary<string, double> { ["商品名稱"] = 300 };

        ColumnWidthService.Save(1, widths1, _filePath);
        ColumnWidthService.Save(2, widths2, _filePath);

        Assert.Equal(200, ColumnWidthService.Load(1, _filePath)["商品名稱"]);
        Assert.Equal(300, ColumnWidthService.Load(2, _filePath)["商品名稱"]);
    }

    [Fact]
    public void Load_ReturnsEmpty_WhenCorruptedFile()
    {
        File.WriteAllText(_filePath, "not valid json!!!");

        var widths = ColumnWidthService.Load(1, _filePath);

        Assert.Empty(widths);
    }

    [Fact]
    public void Load_ReturnsEmpty_WhenCategoryNotInFile()
    {
        var data = new Dictionary<string, Dictionary<string, double>>
        {
            ["1"] = new() { ["商品名稱"] = 200 }
        };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(data));

        var widths = ColumnWidthService.Load(99, _filePath);

        Assert.Empty(widths);
    }
}
