## 1. Database & Model 變更

- [x] 1.1 Product model：移除 Quantity 屬性，新增 ExtraCost (decimal, default 0)
- [x] 1.2 更新 CostPrice 計算公式：`UnitPrice × ExchangeRate × Discount + ListingPrice × (CommissionFee / 100) + ExtraCost`
- [x] 1.3 更新 NotifyComputedProperties：ExtraCost 變更時觸發 CostPrice/Profit/ProfitMargin 通知
- [x] 1.4 新增 Category model（Id, Name），sealed class with init properties
- [x] 1.5 Product 新增 nullable CategoryId FK 與 Category navigation property
- [x] 1.6 AppDbContext 註冊 Category DbSet，設定 Category unique name index，Product→Category FK (SetNull on delete)
- [x] 1.7 建立 EF Core migration（Quantity→ExtraCost 欄位轉換 + Category 新表 + CategoryId FK）

## 2. Category Tab 系統

- [x] 2.1 新增 CategoryViewModel（管理分類 CRUD：新增、刪除、改名）
- [x] 2.2 MainWindowViewModel 改為動態 Tab：維護 ObservableCollection，包含固定幣別 Tab + 動態分類 Tab
- [x] 2.3 每個分類 Tab 建立對應的 ProductListViewModel，篩選 CategoryId
- [x] 2.4 MainWindow.axaml 重構 TabControl 綁定動態 collection
- [x] 2.5 實作新增分類 UI（按鈕 + 輸入對話框）
- [x] 2.6 實作刪除分類 UI（確認對話框；有商品時二次確認）
- [x] 2.7 實作改名分類 UI（雙擊 Tab header 或右鍵選單）

## 3. Product UI 更新

- [x] 3.1 ProductListView.axaml：Quantity 欄位替換為 ExtraCost（NumericUpDown，單位 NT 標示）
- [x] 3.2 DataGrid 欄位更新：移除數量欄，新增額外成本欄
- [x] 3.3 ProfitMargin 欄位顯示格式加上 `%` 後綴
- [x] 3.4 DataGrid 啟用 AlternatingRowBackground（系統配色灰底）
- [x] 3.5 ProductListViewModel：移除 NewQuantity，新增 NewExtraCost，更新 UpdatePricePreview 公式
- [x] 3.6 ProductListViewModel：AddProduct 方法更新欄位對應

## 4. Excel 匯出更新

- [x] 4.1 ExcelExportService：header 欄位更新（數量→額外成本(NT)）
- [x] 4.2 ExcelExportService：資料欄位對應 ExtraCost，ProfitMargin 加 `%`

## 5. 測試更新

- [x] 5.1 Product model 測試：CostPrice 公式含 ExtraCost 的計算驗證
- [x] 5.2 Product model 測試：Profit 和 ProfitMargin 計算驗證
- [x] 5.3 Category CRUD 測試：新增/刪除/改名分類
- [x] 5.4 Category 刪除測試：有商品時 CategoryId 設為 null
- [x] 5.5 Excel 匯出測試：欄位對應更新驗證
