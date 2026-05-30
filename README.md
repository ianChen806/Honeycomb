# Honeycomb 庫存管理系統

![CI](https://github.com/ianChen806/Honeycomb/actions/workflows/ci.yml/badge.svg)
![Release](https://github.com/ianChen806/Honeycomb/actions/workflows/release.yml/badge.svg)

簡易的桌面庫存管理系統，支援多幣別匯率、分類管理與損益計算。

## 功能

- **商品管理**：新增、瀏覽、刪除商品（支援 Shift/Ctrl 複選刪除），DataGrid 直接編輯即時儲存，支援跨分類移動商品
- **商品圖片**：每件商品可設定一張代表圖（新增時或選取後皆可），自動壓縮（最長邊 1024px、JPEG 品質 80）後存入 SQLite，右側面板即時預覽
- **商品搜尋**：依商品名稱即時搜尋，支援上一筆／下一筆導航（Enter／Shift+Enter），且跟隨 DataGrid 目前排序
- **損益計算**：成本價（單價 × 匯率 × 折扣 + 上架價格 × 手續費% + 額外成本）、利潤、利潤率
- **分類管理**：動態新增/刪除/改名分類，各分類獨立 Tab 顯示，右鍵選單操作，拖拉 Tab 調整分類順序
- **欄位寬度記憶**：DataGrid 欄位可拖曳調整寬度，各分類 Tab 獨立保存，關閉時自動記憶
- **幣別設定**：新增、刪除幣別，使用中的幣別無法刪除
- **Excel 匯出**：各分類分不同 Sheet 匯出，含自動過濾與欄寬調整的 .xlsx 檔案

## 技術

- .NET 10 + Avalonia 11 (MVVM)
- SQLite (EF Core)
- SkiaSharp (圖片壓縮)
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
