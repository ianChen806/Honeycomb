## Context

Honeycomb 的分類系統目前以 `Category` model（只有 `Id` 和 `Name`）為基礎，Tab 順序按 `Id` 排列。使用者無法自訂順序。UI 使用 Avalonia `TabControl`，Tab 在 code-behind (`MainWindow.axaml.cs`) 中動態建立。

## Goals / Non-Goals

**Goals:**
- 使用者可以透過拖拉 Tab 來調整分類順序
- 順序持久化到資料庫，重啟後保留
- 「預設」分類也可以被移動位置

**Non-Goals:**
- 鍵盤快捷鍵排序（超出範圍）
- 分類分組或巢狀結構
- 動畫效果（保持簡單）

## Decisions

### 1. SortOrder 欄位設計

**選擇**: Category model 新增 `int SortOrder` 欄位，以 integer 連續編號。

**替代方案**: 使用 float 間隔編號（允許插入不需重排）——對於桌面應用分類數量少，過度設計。

**理由**: 分類數量預期在 10-20 個以內，全量更新 SortOrder 的效能開銷可忽略。

### 2. 拖拉實作方式

**選擇**: 在 `MainWindow.axaml.cs` 使用 Avalonia 內建的 `DragDrop` API（PointerPressed → DragDrop.DoDragDrop → Drop 事件）處理 TabItem 拖拉。

**替代方案**: 使用第三方拖拉套件——引入不必要的依賴。

**理由**: Avalonia 的 DragDrop API 已足夠處理 TabItem 間的拖放，不需要額外依賴。

### 3. 順序持久化時機

**選擇**: 拖拉完成（drop）時立即批次更新所有 Category 的 SortOrder 並 SaveChanges。

**替代方案**: 延遲儲存（例如關閉視窗時）——有遺失風險。

**理由**: 分類數量少，即時儲存的效能影響極小，且確保資料一致性。

### 4. ViewModel 層排序方法

**選擇**: 在 `CategoryViewModel` 新增 `ReorderCategory(int categoryId, int newIndex)` 方法，重算所有 SortOrder 並儲存。

**理由**: 保持 View 層薄、ViewModel 層負責業務邏輯的 MVVM 原則。

## Risks / Trade-offs

- **[Migration]** 既有資料庫的 Category 沒有 SortOrder → Migration 中以現有 Id 順序設定初始 SortOrder
- **[DragDrop 相容性]** Avalonia DragDrop 在不同平台的行為可能略有差異 → 先以 Windows 為主要目標平台
- **[預設分類]** 允許「預設」分類被拖移到非第一位 → 這是預期行為，使用者應有完全的排序自由
