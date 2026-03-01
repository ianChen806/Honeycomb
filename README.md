# Honeycomb 庫存管理系統

簡易的桌面庫存管理系統，支援多幣別與匯率紀錄。

## 功能

- **商品管理**：新增、瀏覽、刪除商品（支援 Shift/Ctrl 複選刪除）
- **自動計算**：成本價（單價 × 折扣）、總價（數量 × 單價 × 匯率 × 折扣）
- **幣別設定**：新增、刪除幣別，使用中的幣別無法刪除
- **Excel 匯出**：匯出含自動過濾與欄寬調整的 .xlsx 檔案

## 技術

- .NET 10 + Avalonia 11 (MVVM)
- SQLite (EF Core)
- ClosedXML (Excel 匯出)
- CommunityToolkit.Mvvm

## 開始使用

```bash
dotnet run --project src/Honeycomb
```

## 測試

```bash
dotnet test
```
