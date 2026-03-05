## Why

目前分類 Tab 的顯示順序是按照資料庫 Id 排列，使用者無法自訂分類的排列順序。當分類數量增多時，使用者需要依照自己的工作流程調整常用分類的位置，例如將最常使用的分類放在最前面。

## What Changes

- Category model 新增 `SortOrder` 欄位，用於控制 Tab 顯示順序
- 分類 Tab 支援滑鼠拖拉（drag-and-drop）重新排序
- 拖拉完成後自動將新順序持久化到資料庫
- 新增分類時自動取得最大 SortOrder + 1，放在最後面

## Capabilities

### New Capabilities
- `category-drag-reorder`: 分類 Tab 的拖拉排序功能，包含 SortOrder 資料模型、拖拉互動、順序持久化

### Modified Capabilities

（無既有 spec 需要修改）

## Impact

- **Model**: `Category` 新增 `SortOrder` 屬性
- **Database**: 需要 EF Core migration 新增 `SortOrder` 欄位
- **ViewModel**: `CategoryViewModel` 排序邏輯改為 `SortOrder`；`MainWindowViewModel.RebuildCategoryTabs` 同步調整
- **View**: `MainWindow.axaml.cs` 中的 `RebuildTabs` 方法需加入拖拉事件處理
- **Dependencies**: 無新增外部依賴，使用 Avalonia 內建的 DragDrop API
