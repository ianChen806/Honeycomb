## Why

目前系統的「數量」欄位對定價流程無實際意義，使用者需要的是能自行填入的「額外成本」（如運費、包材費等以 NT 計價的雜項成本）。同時，隨著商品數量增加，缺乏分類機制讓管理變得困難。需要引入 Tab 分類系統來組織商品，並改善表格的視覺可讀性。

## What Changes

- **BREAKING** 移除 `Quantity` 欄位，替換為 `ExtraCost`（額外成本，單位 NT，使用者自行填入）
- **BREAKING** 成本公式更新：`CostPrice = UnitPrice × ExchangeRate × Discount + ListingPrice × (CommissionFee / 100) + ExtraCost`（額外成本不受匯率影響）
- 毛利率（ProfitMargin）顯示格式加上 `%` 後綴
- DataGrid 啟用交替列底色（白底/灰底，跟隨系統配色）
- 新增 Category（大分類）概念，每個分類獨立一個 Tab
- 幣別設定維持共用一個 Tab，不隸屬於任何分類
- 使用者可手動新增、刪除、改名分類 Tab
- 刪除分類前需確認；若分類下有商品需二次確認

## Capabilities

### New Capabilities
- `category-tabs`: 大分類 Tab 系統——新增/刪除/改名分類，每個分類獨立 Tab 與儲存內容，刪除前確認機制

### Modified Capabilities
- `product-management`: Quantity 替換為 ExtraCost、成本公式更新、毛利率顯示加 `%`、DataGrid 交替列底色
- `excel-export`: 匯出欄位需對應 Quantity→ExtraCost 的變更，毛利率加 `%`

## Impact

- **Models**: Product 移除 Quantity、新增 ExtraCost；新增 Category model
- **Database**: 需要 EF Core migration（欄位變更 + 新表）
- **ViewModels**: ProductListViewModel 公式邏輯更新；新增 CategoryViewModel；MainWindowViewModel 改為動態 Tab
- **Views**: ProductListView 欄位/表單更新、DataGrid 樣式；MainWindow Tab 系統重構
- **Services**: ExcelExportService 欄位對應更新
- **Tests**: 所有涉及 Quantity、CostPrice 公式的測試需更新
