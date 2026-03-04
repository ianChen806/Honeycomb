## Context

Honeycomb 是 Avalonia 11 MVVM 桌面庫存管理應用。目前 DataGrid 欄位寬度在 XAML 中寫死，手續費欄位數值不帶 `%` 符號，商品無法跨分類移動。視窗大小已有持久化機制（`%LocalAppData%/Honeycomb/window.json`），可參考其模式。

## Goals / Non-Goals

**Goals:**
- 手續費儲存格數值顯示 `%` 後綴
- 每個分類 Tab 的 DataGrid 欄位寬度可拖曳調整並獨立保存
- 提供「移動到分類」按鈕，選取商品可批次移至其他分類

**Non-Goals:**
- 不做欄位排序順序保存
- 不做欄位顯示/隱藏切換
- 不做跨分類複製（僅移動）

## Decisions

### 1. 手續費 StringFormat 修改

直接修改 `ProductListView.axaml` 中手續費欄位的 `StringFormat` 從 `0` 改為 `{}{0:0}%`，與利潤率欄位保持一致風格。

### 2. 欄位寬度持久化 — JSON 檔案 per Tab

**方案**: 寫入 `%LocalAppData%/Honeycomb/column-widths.json`，結構為 `{ [categoryId]: { [columnHeader]: width } }`。

**理由**:
- 與現有 `window.json` 模式一致
- 單一檔案管理所有 Tab 的欄位寬度
- JSON 可讀性好，方便除錯

**替代方案（不採用）**:
- SQLite 表：過度設計，欄位寬度不是業務資料
- 每 Tab 一個檔案：檔案過多，管理複雜

**實作方式**:
- 新增 `ColumnWidthService` 靜態類別，提供 `Save(categoryId, widths)` 和 `Load(categoryId)` 方法
- `ProductListView` 在 `Loaded` 事件還原寬度，在 `MainWindow.Closing` 時保存寬度
- 監聽 DataGrid 的 `ColumnHeader` 拖曳結束事件觸發保存

### 3. 移動商品到分類 — 對話框 + ViewModel 方法

**方案**: 新增 `MoveCategoryDialog` 視窗（Avalonia Window），包含分類 ComboBox 與確認/取消按鈕。

**理由**:
- Avalonia 沒有內建 MessageBox 可選擇分類
- 獨立 Window 可以用 `ShowDialog<T>` 取得結果

**流程**:
1. 使用者選取商品 → 點「移動到分類」按鈕
2. 彈出 `MoveCategoryDialog`，顯示除當前分類外的所有分類
3. 使用者選擇目標分類 → 確認
4. `ProductListViewModel.MoveProducts(products, targetCategoryId)` 更新 CategoryId 並儲存
5. 重新載入當前 Tab 的商品列表

## Risks / Trade-offs

- **[欄位寬度檔案損壞]** → 靜默忽略，回退到 XAML 預設寬度（與 window.json 處理方式一致）
- **[移動商品後未刷新其他 Tab]** → 僅刷新當前 Tab，目標 Tab 在切換時自然重新載入
- **[大量商品移動效能]** → 批次更新 CategoryId 後單次 SaveChanges，可接受
