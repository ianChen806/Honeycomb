using System.Collections.Generic;
using ClosedXML.Excel;
using Honeycomb.Models;

namespace Honeycomb.Services;

public sealed class ExcelExportService
{
    public void Export(IReadOnlyList<Product> products, string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("庫存清單");

        var headers = new[]
        {
            "商品名稱", "數量", "單價", "幣別", "匯率",
            "折扣", "成本價", "總價", "建立時間"
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
            worksheet.Cell(r, 2).Value = p.Quantity;
            worksheet.Cell(r, 3).Value = p.UnitPrice;
            worksheet.Cell(r, 4).Value = p.Currency?.Code ?? "";
            worksheet.Cell(r, 5).Value = p.ExchangeRate;
            worksheet.Cell(r, 6).Value = p.Discount;
            worksheet.Cell(r, 7).Value = p.CostPrice;
            worksheet.Cell(r, 8).Value = p.TotalPrice;
            worksheet.Cell(r, 9).Value = p.CreatedAt.ToString("yyyy/MM/dd");
        }

        var dataRange = worksheet.RangeUsed()!;
        dataRange.SetAutoFilter();
        worksheet.Columns().AdjustToContents(minWidth: 8, maxWidth: 50);
        workbook.SaveAs(filePath);
    }
}
