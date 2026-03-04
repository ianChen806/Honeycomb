using System.Collections.Generic;
using ClosedXML.Excel;
using Honeycomb.Models;

namespace Honeycomb.Services;

public sealed class ExcelExportService
{
    public void Export(IReadOnlyList<Product> products, string filePath)
    {
        Export([("庫存清單", products)], filePath);
    }

    public void Export(IReadOnlyList<(string SheetName, IReadOnlyList<Product> Products)> sheets, string filePath)
    {
        using var workbook = new XLWorkbook();

        foreach (var (sheetName, products) in sheets)
        {
            WriteSheet(workbook, sheetName, products);
        }

        workbook.SaveAs(filePath);
    }

    private static void WriteSheet(XLWorkbook workbook, string sheetName, IReadOnlyList<Product> products)
    {
        var worksheet = workbook.Worksheets.Add(sheetName);

        var headers = new[]
        {
            "商品名稱", "單價", "幣別", "匯率", "額外成本",
            "折扣", "上架價格", "手續費(%)", "成本價", "利潤", "利潤率(%)", "建立時間"
        };

        for (var col = 0; col < headers.Length; col++)
        {
            worksheet.Cell(1, col + 1).Value = headers[col];
        }

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;

        for (var row = 0; row < products.Count; row++)
        {
            var p = products[row];
            var r = row + 2;
            worksheet.Cell(r, 1).Value = p.Name;
            worksheet.Cell(r, 2).Value = p.UnitPrice;
            worksheet.Cell(r, 3).Value = p.Currency?.Code ?? "";
            worksheet.Cell(r, 4).Value = p.ExchangeRate;
            worksheet.Cell(r, 5).Value = p.ExtraCost;
            worksheet.Cell(r, 6).Value = p.Discount;
            worksheet.Cell(r, 7).Value = p.ListingPrice;
            worksheet.Cell(r, 8).Value = p.CommissionFee;
            worksheet.Cell(r, 9).Value = p.CostPrice;
            worksheet.Cell(r, 10).Value = p.Profit;
            worksheet.Cell(r, 11).Value = $"{p.ProfitMargin:N2}%";
            worksheet.Cell(r, 12).Value = p.CreatedAt.ToString("yyyy/MM/dd");
        }

        if (products.Count > 0)
        {
            var dataRange = worksheet.RangeUsed()!;
            dataRange.SetAutoFilter();
        }

        worksheet.Columns().AdjustToContents(minWidth: 8, maxWidth: 50);
    }
}
