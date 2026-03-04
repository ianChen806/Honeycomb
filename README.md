# Honeycomb 庫存管理系統

![CI](https://github.com/ianChen806/Honeycomb/actions/workflows/ci.yml/badge.svg)
![Release](https://github.com/ianChen806/Honeycomb/actions/workflows/release.yml/badge.svg)

簡易的桌面庫存管理系統，支援多幣別匯率、分類管理與損益計算。

## 功能

- **商品管理**：新增、瀏覽、刪除商品（支援 Shift/Ctrl 複選刪除），DataGrid 直接編輯即時儲存
- **損益計算**：成本價（單價 × 匯率 × 折扣 + 上架價格 × 手續費% + 額外成本）、利潤、利潤率
- **分類管理**：動態新增/刪除/改名分類，各分類獨立 Tab 顯示，右鍵選單操作
- **幣別設定**：新增、刪除幣別，使用中的幣別無法刪除
- **Excel 匯出**：各分類分不同 Sheet 匯出，含自動過濾與欄寬調整的 .xlsx 檔案

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
