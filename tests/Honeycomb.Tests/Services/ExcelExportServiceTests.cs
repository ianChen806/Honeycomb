using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using Honeycomb.Models;
using Honeycomb.Services;

namespace Honeycomb.Tests.Services;

public class ExcelExportServiceTests : IDisposable
{
    private readonly string _tempFile;
    private readonly ExcelExportService _service = new();

    public ExcelExportServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.xlsx");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Fact]
    public void Export_CreatesFile_WithCorrectHeaders()
    {
        var products = new List<Product>
        {
            new()
            {
                Name = "Widget",
                Quantity = 10,
                UnitPrice = 100m,
                CurrencyId = 1,
                Currency = new Currency { Id = 1, Code = "USD", Name = "美元" },
                ExchangeRate = 31.5m,
                Discount = 0.9m
            }
        };

        _service.Export(products, _tempFile);

        Assert.True(File.Exists(_tempFile));

        using var workbook = new XLWorkbook(_tempFile);
        var ws = workbook.Worksheet(1);
        Assert.Equal("商品名稱", ws.Cell(1, 1).GetString());
        Assert.Equal("數量", ws.Cell(1, 2).GetString());
        Assert.Equal("單價", ws.Cell(1, 3).GetString());
        Assert.Equal("幣別", ws.Cell(1, 4).GetString());
        Assert.Equal("匯率", ws.Cell(1, 5).GetString());
        Assert.Equal("折扣", ws.Cell(1, 6).GetString());
        Assert.Equal("成本價", ws.Cell(1, 7).GetString());
        Assert.Equal("總價", ws.Cell(1, 8).GetString());
        Assert.Equal("建立時間", ws.Cell(1, 9).GetString());
    }

    [Fact]
    public void Export_WritesProductData_Correctly()
    {
        var products = new List<Product>
        {
            new()
            {
                Name = "Gadget",
                Quantity = 5,
                UnitPrice = 200m,
                CurrencyId = 1,
                Currency = new Currency { Id = 1, Code = "JPY", Name = "日圓" },
                ExchangeRate = 0.22m,
                Discount = 0.85m
            }
        };

        _service.Export(products, _tempFile);

        using var workbook = new XLWorkbook(_tempFile);
        var ws = workbook.Worksheet(1);

        Assert.Equal("Gadget", ws.Cell(2, 1).GetString());
        Assert.Equal(5, ws.Cell(2, 2).GetValue<int>());
        Assert.Equal(200m, ws.Cell(2, 3).GetValue<decimal>());
        Assert.Equal("JPY", ws.Cell(2, 4).GetString());
        Assert.Equal(0.22m, ws.Cell(2, 5).GetValue<decimal>());
        Assert.Equal(0.85m, ws.Cell(2, 6).GetValue<decimal>());
        Assert.Equal(170m, ws.Cell(2, 7).GetValue<decimal>()); // 200 * 0.85
        Assert.Equal(187m, ws.Cell(2, 8).GetValue<decimal>()); // 5 * 200 * 0.22 * 0.85
    }

    [Fact]
    public void Export_EmptyList_CreatesFileWithHeadersOnly()
    {
        _service.Export(new List<Product>(), _tempFile);

        Assert.True(File.Exists(_tempFile));

        using var workbook = new XLWorkbook(_tempFile);
        var ws = workbook.Worksheet(1);
        Assert.Equal("商品名稱", ws.Cell(1, 1).GetString());
        Assert.True(ws.Cell(2, 1).IsEmpty());
    }

    [Fact]
    public void Export_MultipleProducts_WritesAllRows()
    {
        var currency = new Currency { Id = 1, Code = "USD", Name = "美元" };
        var products = new List<Product>
        {
            new() { Name = "A", Quantity = 1, UnitPrice = 10m, CurrencyId = 1, Currency = currency, ExchangeRate = 1m, Discount = 1m },
            new() { Name = "B", Quantity = 2, UnitPrice = 20m, CurrencyId = 1, Currency = currency, ExchangeRate = 1m, Discount = 0.9m },
            new() { Name = "C", Quantity = 3, UnitPrice = 30m, CurrencyId = 1, Currency = currency, ExchangeRate = 1m, Discount = 0.8m }
        };

        _service.Export(products, _tempFile);

        using var workbook = new XLWorkbook(_tempFile);
        var ws = workbook.Worksheet(1);
        Assert.Equal("A", ws.Cell(2, 1).GetString());
        Assert.Equal("B", ws.Cell(3, 1).GetString());
        Assert.Equal("C", ws.Cell(4, 1).GetString());
        Assert.True(ws.Cell(5, 1).IsEmpty());
    }
}
