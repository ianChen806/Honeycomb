## Why

需要一個簡易的桌面庫存管理系統，能夠記錄商品資訊並追蹤不同幣別的成本與匯率。目前沒有統一的工具來管理多幣別商品成本計算，手動使用試算表容易出錯且缺乏結構化管理。

## What Changes

- 新增 Avalonia 桌面應用程式（.NET 10）
- 商品管理功能：新增、瀏覽、刪除商品，包含名稱、數量、單價、幣別、匯率、折扣
- 自動計算成本價（單價 × 折扣）與總價（數量 × 單價 × 匯率 × 折扣）
- 幣別設定功能：新增、刪除可用幣別
- Excel 匯出功能：將所有商品資料匯出為 .xlsx 檔案
- SQLite 本地資料儲存

## Capabilities

### New Capabilities

- `product-management`: 商品的新增、瀏覽、刪除，包含成本價與總價的自動計算
- `currency-settings`: 幣別的新增、刪除與管理，供商品選擇使用
- `excel-export`: 將商品清單匯出為 Excel 檔案

### Modified Capabilities

（無既有 capabilities）

## Impact

- 全新專案，無既有程式碼受影響
- 依賴套件：Avalonia 11.x、EF Core（SQLite）、CommunityToolkit.Mvvm、ClosedXML
- 本地 SQLite 資料庫檔案
