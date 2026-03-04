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
                ExtraCost = 0,
                UnitPrice = 100m,
                CurrencyId = 1,
                Currency = new Currency { Id = 1, Code = "USD", Name = "美元" },
                ExchangeRate = 31.5m,
                Discount = 0.9m,
                ListingPrice = 5000,
                CommissionFee = 15
            }
        };

        _service.Export(products, _tempFile);

        Assert.True(File.Exists(_tempFile));

        using var workbook = new XLWorkbook(_tempFile);
        var ws = workbook.Worksheet(1);
        Assert.Equal("商品名稱", ws.Cell(1, 1).GetString());
        Assert.Equal("單價", ws.Cell(1, 2).GetString());
        Assert.Equal("幣別", ws.Cell(1, 3).GetString());
        Assert.Equal("匯率", ws.Cell(1, 4).GetString());
        Assert.Equal("額外成本", ws.Cell(1, 5).GetString());
        Assert.Equal("折扣", ws.Cell(1, 6).GetString());
        Assert.Equal("上架價格", ws.Cell(1, 7).GetString());
        Assert.Equal("手續費(%)", ws.Cell(1, 8).GetString());
        Assert.Equal("成本價", ws.Cell(1, 9).GetString());
        Assert.Equal("利潤", ws.Cell(1, 10).GetString());
        Assert.Equal("利潤率(%)", ws.Cell(1, 11).GetString());
        Assert.Equal("建立時間", ws.Cell(1, 12).GetString());
    }

    [Fact]
    public void Export_WritesProductData_Correctly()
    {
        var products = new List<Product>
        {
            new()
            {
                Name = "Gadget",
                ExtraCost = 0,
                UnitPrice = 200m,
                CurrencyId = 1,
                Currency = new Currency { Id = 1, Code = "JPY", Name = "日圓" },
                ExchangeRate = 0.22m,
                Discount = 0.85m,
                ListingPrice = 500,
                CommissionFee = 10
            }
        };

        _service.Export(products, _tempFile);

        using var workbook = new XLWorkbook(_tempFile);
        var ws = workbook.Worksheet(1);

        Assert.Equal("Gadget", ws.Cell(2, 1).GetString());
        Assert.Equal(200m, ws.Cell(2, 2).GetValue<decimal>());
        Assert.Equal("JPY", ws.Cell(2, 3).GetString());
        Assert.Equal(0.22m, ws.Cell(2, 4).GetValue<decimal>());
        Assert.Equal(0m, ws.Cell(2, 5).GetValue<decimal>());
        Assert.Equal(0.85m, ws.Cell(2, 6).GetValue<decimal>());
        Assert.Equal(500m, ws.Cell(2, 7).GetValue<decimal>());
        Assert.Equal(10m, ws.Cell(2, 8).GetValue<decimal>());
        // CostPrice = 200*0.22*0.85 + 500*(10/100) + 0 = 37.4 + 50 = 87.4
        Assert.Equal(87.4m, ws.Cell(2, 9).GetValue<decimal>());
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
    public void Export_ProfitMargin_DisplaysWithPercentSign()
    {
        var products = new List<Product>
        {
            new()
            {
                Name = "Test",
                ExtraCost = 0,
                UnitPrice = 100m,
                CurrencyId = 1,
                Currency = new Currency { Id = 1, Code = "USD", Name = "美元" },
                ExchangeRate = 1m,
                Discount = 1m,
                ListingPrice = 200,
                CommissionFee = 0
            }
        };

        _service.Export(products, _tempFile);

        using var workbook = new XLWorkbook(_tempFile);
        var ws = workbook.Worksheet(1);
        // ProfitMargin = (100/200)*100 = 50.00
        Assert.Equal("50.00%", ws.Cell(2, 11).GetString());
    }

    [Fact]
    public void Export_ExtraCost_WrittenCorrectly()
    {
        var products = new List<Product>
        {
            new()
            {
                Name = "Test",
                ExtraCost = 250,
                UnitPrice = 100m,
                CurrencyId = 1,
                Currency = new Currency { Id = 1, Code = "USD", Name = "美元" },
                ExchangeRate = 1m,
                Discount = 1m,
                ListingPrice = 500,
                CommissionFee = 10
            }
        };

        _service.Export(products, _tempFile);

        using var workbook = new XLWorkbook(_tempFile);
        var ws = workbook.Worksheet(1);
        Assert.Equal(250m, ws.Cell(2, 5).GetValue<decimal>());
        // CostPrice = 100*1*1 + 500*(10/100) + 250 = 100 + 50 + 250 = 400
        Assert.Equal(400m, ws.Cell(2, 9).GetValue<decimal>());
    }

    [Fact]
    public void Export_MultiSheet_CreatesSeparateSheets()
    {
        var currency = new Currency { Id = 1, Code = "USD", Name = "美元" };
        var sheets = new List<(string SheetName, IReadOnlyList<Product> Products)>
        {
            ("預設", new List<Product>
            {
                new() { Name = "A", UnitPrice = 10m, CurrencyId = 1, Currency = currency, ExchangeRate = 1m, Discount = 1m }
            }),
            ("日用品", new List<Product>
            {
                new() { Name = "B", UnitPrice = 20m, CurrencyId = 1, Currency = currency, ExchangeRate = 1m, Discount = 1m },
                new() { Name = "C", UnitPrice = 30m, CurrencyId = 1, Currency = currency, ExchangeRate = 1m, Discount = 1m }
            })
        };

        _service.Export(sheets, _tempFile);

        using var workbook = new XLWorkbook(_tempFile);
        Assert.Equal(2, workbook.Worksheets.Count);
        Assert.Equal("預設", workbook.Worksheets.Worksheet(1).Name);
        Assert.Equal("日用品", workbook.Worksheets.Worksheet(2).Name);

        Assert.Equal("A", workbook.Worksheets.Worksheet(1).Cell(2, 1).GetString());
        Assert.Equal("B", workbook.Worksheets.Worksheet(2).Cell(2, 1).GetString());
        Assert.Equal("C", workbook.Worksheets.Worksheet(2).Cell(3, 1).GetString());
    }

    [Fact]
    public void Export_MultipleProducts_WritesAllRows()
    {
        var currency = new Currency { Id = 1, Code = "USD", Name = "美元" };
        var products = new List<Product>
        {
            new() { Name = "A", ExtraCost = 0, UnitPrice = 10m, CurrencyId = 1, Currency = currency, ExchangeRate = 1m, Discount = 1m },
            new() { Name = "B", ExtraCost = 50, UnitPrice = 20m, CurrencyId = 1, Currency = currency, ExchangeRate = 1m, Discount = 0.9m },
            new() { Name = "C", ExtraCost = 100, UnitPrice = 30m, CurrencyId = 1, Currency = currency, ExchangeRate = 1m, Discount = 0.8m }
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
