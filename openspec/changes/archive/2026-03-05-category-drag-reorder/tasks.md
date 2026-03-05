## 1. Data Model & Migration

- [x] 1.1 在 `Category` model 新增 `SortOrder` 屬性 (`int`, default 0)
- [x] 1.2 在 `AppDbContext.OnModelCreating` 設定 `SortOrder` 欄位配置，更新 seed data
- [x] 1.3 新增 EF Core migration (`AddCategorySortOrder`)，在 migration 中以 Id 順序指派初始 SortOrder

## 2. ViewModel 排序邏輯

- [x] 2.1 修改 `CategoryViewModel.LoadCategories()` 改為 `OrderBy(c => c.SortOrder)`
- [x] 2.2 修改 `CategoryViewModel.AddCategory()` 新增分類時設定 SortOrder 為當前最大值 + 1
- [x] 2.3 在 `CategoryViewModel` 新增 `ReorderCategory(int categoryId, int newIndex)` 方法，重算所有 SortOrder 並儲存
- [x] 2.4 修改 `MainWindowViewModel.RebuildCategoryTabs()` 改為 `OrderBy(c => c.SortOrder)`

## 3. View 拖拉互動

- [x] 3.1 在 `MainWindow.axaml.cs` 的 `RebuildTabs` 中為每個 TabItem 註冊 PointerPressed 事件啟動拖拉
- [x] 3.2 實作 Drop 事件處理：計算目標位置，呼叫 `CategoryViewModel.ReorderCategory`
- [x] 3.3 設定 `DragDrop.AllowDrop` 並處理 DragOver 事件以顯示拖拉游標

## 4. 測試

- [x] 4.1 測試 `ReorderCategory` 方法正確重算 SortOrder
- [x] 4.2 測試新增分類自動取得正確的 SortOrder
- [x] 4.3 測試 `RebuildCategoryTabs` 按 SortOrder 排序
