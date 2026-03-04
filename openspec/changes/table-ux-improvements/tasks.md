## 1. 手續費顯示格式

- [x] 1.1 修改 `ProductListView.axaml` 手續費欄位 StringFormat 從 `0` 改為 `{}{0:0}%`

## 2. 欄位寬度持久化

- [x] 2.1 新增 `ColumnWidthService` 靜態類別（Load/Save 方法，讀寫 `column-widths.json`）
- [x] 2.2 修改 `ProductListView.axaml` DataGrid 欄位加入 `CanUserResize="True"`
- [x] 2.3 `ProductListView` code-behind 加入 Loaded 事件還原欄位寬度
- [x] 2.4 `MainWindow` Closing 事件觸發所有 Tab 的欄位寬度保存
- [x] 2.5 撰寫 `ColumnWidthService` 單元測試

## 3. 移動商品到分類

- [x] 3.1 新增 `MoveCategoryDialog.axaml` / `.axaml.cs`（ComboBox + 確認/取消按鈕）
- [x] 3.2 `ProductListView.axaml` 在刪除按鈕旁新增「移動到分類」按鈕
- [x] 3.3 `ProductListView` code-behind 實作按鈕點擊事件（開啟 Dialog、取得結果）
- [x] 3.4 `ProductListViewModel` 新增 `MoveProducts(products, targetCategoryId)` 方法
- [x] 3.5 撰寫 `MoveProducts` 單元測試
