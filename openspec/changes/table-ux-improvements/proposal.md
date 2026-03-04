## Why

目前表格的手續費欄位數值沒有顯示 `%` 符號，使用者需要靠記憶判斷單位。欄位寬度固定無法調整，不同分類 Tab 無法保存各自偏好的欄位配置。此外，商品只能刪除而無法在分類間移動，使用者需要先刪再重建，造成不必要的操作負擔。

## What Changes

- 手續費欄位的儲存格數值顯示加上 `%` 後綴（例如 `10` → `10%`）
- DataGrid 欄位支援使用者拖曳調整寬度，每個分類 Tab 獨立記憶欄位寬度，應用程式關閉時自動保存、啟動時還原
- 刪除按鈕旁新增「移動到分類」按鈕，點擊後彈出對話框讓使用者選擇目標分類，將選取的商品批次移動

## Capabilities

### New Capabilities
- `column-width-persistence`: DataGrid 欄位寬度的儲存與還原機制，按分類 Tab 獨立保存至本機設定檔
- `product-move-category`: 選取商品批次移動到其他分類的功能，包含目標分類選擇對話框

### Modified Capabilities
- `product-management`: 手續費欄位 StringFormat 修改為顯示 `%` 後綴

## Impact

- **Views**: `ProductListView.axaml` — DataGrid 欄位寬度屬性與手續費 StringFormat 變更；新增「移動到分類」按鈕與 code-behind
- **Views**: 新增 `MoveCategoryDialog` 對話框（選擇目標分類的彈窗）
- **ViewModels**: `ProductListViewModel` — 新增 MoveProducts 方法與欄位寬度儲存/還原邏輯
- **Services**: 新增或擴充欄位寬度持久化服務（寫入 `%LocalAppData%/Honeycomb/` 下的設定檔）
- **Models**: Product.CategoryId 變更邏輯（已有欄位，僅需更新值）
