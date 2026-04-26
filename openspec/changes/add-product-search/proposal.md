## Why

當分類內商品數量增多時，使用者必須手動捲動 DataGrid 才能找到特定商品，效率低落。加入 Ctrl+F 搜尋讓使用者沿用瀏覽器肌肉記憶快速定位商品列。

## What Changes

- 在 `ProductListView` 加入浮動搜尋層（右上角 Border，預設隱藏，由 `IsSearchVisible` 控制）
- 支援 `Ctrl+F` 開啟、`Esc` 關閉的鍵盤快捷鍵
- 即時依商品名稱（不分大小寫）過濾匹配結果，匹配列透過 `SelectedItem` + `ScrollIntoView` 高亮並捲入視野
- 支援 `Enter` / `Shift+Enter` 在匹配結果間前進／後退導航，到達邊界 wrap-around
- 顯示「目前/總計」匹配計數（例如 `3/15`）
- 搜尋範圍限定為當前分類 Tab；切 Tab、`LoadData()`、新增／刪除／移動商品會自動清空搜尋狀態

## Capabilities

### New Capabilities

(無)

### Modified Capabilities

- `product-management`: 新增「使用者可依商品名稱搜尋」的需求，包含浮動搜尋層、鍵盤導航、匹配計數、自動重置等情境

## Impact

- **Code**:
  - `src/Honeycomb/ViewModels/ProductListViewModel.cs`：新增搜尋狀態、匹配與導航邏輯、`MatchScrollRequested` 事件、`OpenSearchCommand`／`CloseSearch`、`LoadData` 內呼叫重置
  - `src/Honeycomb/Views/ProductListView.axaml`：新增浮動 Border + `UserControl.KeyBindings`
  - `src/Honeycomb/Views/ProductListView.axaml.cs`：訂閱 `MatchScrollRequested`、處理 `Enter`/`Shift+Enter`/`Esc` 鍵盤事件、`OpenSearch` 後 focus SearchBox
- **Tests**: 新增 `tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs`（12 個測試案例）
- **Dependencies**: 無新增；沿用現有 CommunityToolkit.Mvvm + Avalonia 11
- **DB / Migrations**: 無
- **APIs**: 無公開 API 變動
