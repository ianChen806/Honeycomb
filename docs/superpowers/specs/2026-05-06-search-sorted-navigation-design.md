# 商品搜尋導航跟隨排序設計規格

**日期：** 2026-05-06
**範圍：** Honeycomb 庫存管理系統 — `ProductListView` 浮動搜尋層的導航順序

## 問題敘述

目前 `ProductListView` 的 Ctrl+F 搜尋導航（↑/↓、Enter / Shift+Enter）走的是 `Products` ObservableCollection 的順序，也就是「資料庫載入順序」。這跟使用者在 DataGrid 上實際看到的排序不一致：

- 啟動時 `OnLoaded` 會主動把第一欄 set 為 `Ascending`（`ProductListView.axaml.cs:27`）
- 使用者也可以 click 任一欄 header 切排序
- 但 `ProductListViewModel.RecomputeMatches()`（`ProductListViewModel.cs:303-323`）直接從 `Products` 用 `.Where(...)` 篩，沒走 DataGrid 上的排序視圖

結果：使用者按 ↓ 跳到的不是「畫面上的下一筆 match」，而是資料來源的下一筆，與肉眼順序對不上。

## 目標

讓搜尋導航的「下一筆 / 上一筆」順序與 DataGrid 當下顯示的排序一致；搜尋過程中使用者切換排序時，**保留目前選中的那一筆作為新的索引位置**。

## 規格決策摘要

| 項目 | 決策 |
|------|------|
| 排序順序的權威來源 | View（`DataGrid.CollectionView`） |
| 注入方式 | View 設定 VM 上的 `OrderedProductsProvider: Func<IEnumerable<Product>>?` |
| 搜尋中切排序 → 索引處理 | 找回原本選中那筆在新序列的 index；找不到則 fallback 到 0 |
| 切排序時是否強制 scroll | **不**強制 scroll（使用者焦點在欄 header，避免畫面打架） |
| 多欄排序支援 | 不在這次範圍（同桌平常單欄就夠） |
| 編輯 Name 即時更新 match | 不在這次範圍（已知限制） |

## 架構

### 動到的檔案

- `src/Honeycomb/ViewModels/ProductListViewModel.cs` — 加入 `OrderedProductsProvider` 與 `OnSortChanged()`，調整 `RecomputeMatches()` 的資料來源
- `src/Honeycomb/Views/ProductListView.axaml.cs` — 訂閱 `ProductGrid.Sorting`，提供 `OrderedProductsProvider`
- `tests/Honeycomb.Tests/ProductSearchNavigationTests.cs` — 新增單元測試

### MVVM 分層

```
┌─────────────────────────────────────────────────────┐
│ ProductListView (View)                              │
│  ├─ ProductGrid.Sorting 事件 ─→ vm.OnSortChanged()  │
│  └─ OrderedProductsProvider ←── 從 DataGrid         │
│     回傳 DataGrid 排序後的 Product 序列             │
└─────────────────────┬───────────────────────────────┘
                      │  Func<IEnumerable<Product>>
                      ▼
┌─────────────────────────────────────────────────────┐
│ ProductListViewModel (VM)                           │
│  ├─ SearchQuery / IsSearchVisible / MatchCountText  │
│  ├─ _matches: List<Product>  ← 依 View 提供的順序   │
│  ├─ _currentMatchIndex                              │
│  ├─ OrderedProductsProvider: Func<...>?  ← View 注入│
│  ├─ RecomputeMatches() —— 從 provider 取序列        │
│  ├─ OnSortChanged() —— 重算 + 保留選中那筆          │
│  └─ NextMatch / PreviousMatch                       │
└─────────────────────────────────────────────────────┘
```

「DataGrid 上看到的排序」是 UI 狀態，本來就住在 View；VM 透過薄薄的 `Func<IEnumerable<Product>>` 介面借用，依賴方向（View → VM 注入）正確，不違反 MVVM。

## 元件介面

### `ProductListViewModel` 新增 / 變更

```csharp
// 新增屬性（View 注入；測試也可手動設定）
public Func<IEnumerable<Product>>? OrderedProductsProvider { get; set; }

// 新增方法（View 在 DataGrid Sorting 完成後呼叫）
public void OnSortChanged();

// 內部行為變更（簽名不變）
private void RecomputeMatches();
//   舊：_matches = Products.Where(...).ToList()
//   新：var source = OrderedProductsProvider?.Invoke() ?? Products
//       _matches = source.Where(...).ToList()
```

### `ProductListView` 新增訂閱與方法

```csharp
// OnAttachedToVisualTree 內補：
vm.OrderedProductsProvider = () => GetOrderedProducts();
ProductGrid.Sorting += OnGridSorting;

// OnDetachedFromVisualTree 內補：
vm.OrderedProductsProvider = null;
ProductGrid.Sorting -= OnGridSorting;

// 新方法
private IEnumerable<Product> GetOrderedProducts();
//   從 ProductGrid.CollectionView (Avalonia 內建 DataGridCollectionView) 取已排序序列

private void OnGridSorting(object?, DataGridColumnEventArgs);
//   Sorting 事件在排序「即將套用」前觸發
//   用 Dispatcher.UIThread.Post(() => vm.OnSortChanged())
//   讓 DataGrid 內部排好之後才讓 VM 重算
```

### 為什麼是 provider 而不是直接傳 list

DataGrid 的 `CollectionView` 是 live view，每次取會反映當下排序狀態。VM 需要的是「現在這一刻的順序」，所以注入 lazy `Func<>` 比 cache 一個 list 安全。

## 資料流

### 情境 1：使用者輸入新 SearchQuery

```
TextBox.Text 變動
  → SearchQuery setter
  → OnSearchQueryChanged
  → RecomputeMatches()
      ├─ source = OrderedProductsProvider() ?? Products
      ├─ _matches = source.Where(p => p.Name.Contains(q, IgnoreCase)).ToList()
      ├─ _currentMatchIndex = (_matches.Count > 0) ? 0 : -1
      ├─ UpdateMatchCountText()
      └─ MatchScrollRequested?.Invoke(_matches[0])  // 若有命中
```

### 情境 2：使用者按 ↑ / ↓

沿用現況，不動：

```
NextMatch / PreviousMatch
  → _currentMatchIndex 在 _matches 內循環 (% _matches.Count)
  → MatchScrollRequested?.Invoke(_matches[index])
  → View.OnMatchScrollRequested
      ├─ ProductGrid.SelectedItem = product
      └─ ProductGrid.ScrollIntoView(product, null)
```

### 情境 3：使用者切換排序欄位（核心新邏輯）

```
User click DataGridColumn header
  → ProductGrid.Sorting 事件 (排序「即將套用」前)
  → View.OnGridSorting
      └─ Dispatcher.UIThread.Post(() => vm.OnSortChanged())
          // 排到下一個 UI tick，等 DataGrid 內部排序套用完成
  → vm.OnSortChanged()
      ├─ if (_matches.Count == 0) return    // 沒有搜尋中，不處理
      ├─ var prevSelected = (_currentMatchIndex >= 0)
      │     ? _matches[_currentMatchIndex] : null
      ├─ // 重新依新順序計算 matches
      │  var source = OrderedProductsProvider?.Invoke() ?? Products
      │  _matches = source.Where(p => p.Name.Contains(q, IgnoreCase)).ToList()
      ├─ // 在新 _matches 找回原本選中那筆
      │  var newIndex = prevSelected is null
      │     ? 0
      │     : _matches.IndexOf(prevSelected)
      │  _currentMatchIndex = (newIndex >= 0) ? newIndex
      │     : (_matches.Count > 0 ? 0 : -1)
      ├─ UpdateMatchCountText()
      └─ // 不觸發 MatchScrollRequested
         //   理由：使用者剛在動排序，UI 焦點在欄 header，
         //   再強制 scroll 會打架；保留索引正確就夠
```

### 情境 4：搜尋框關閉、空 query 時切排序

`OnSortChanged` 內 `_matches.Count == 0` 早退，無副作用。

### 情境 5：選中那筆從新序列消失（罕見）

例如同時被刪、或改名後不再 match：`_matches.IndexOf(prevSelected)` 回 -1 → fallback 到 index 0（若有 match）或 -1（沒有）。

### 隱含前提：`Product` 的 reference equality

`_matches.IndexOf(prevSelected)` 沒有自訂 comparer，靠的是 `Product` 的 default reference equality（`ObservableObject` 未 override `Equals`）。這在這次情境是安全的：`Products` ObservableCollection 裡的 instances 由 EF Core 追蹤而來，`OrderedProductsProvider` 取得的序列只是用同樣的 instances 排個順序，所以重算前後 prevSelected 與新 `_matches` 內對應那筆是同一個 reference。如果未來改成「每次重新 query DB 拿新 instance」，這個假設會失效，需要切換成 `Id` 比對。

## 錯誤處理 / 邊界

| 邊界 | 處理 |
|---|---|
| `OrderedProductsProvider == null`（unit test 或 View 還沒掛上） | Fallback 到 `Products`，行為等同原本 |
| `OnSortChanged` 在 `_matches.Count == 0` 時被呼叫 | 早退，無動作 |
| 重算後新 `_matches` 為空 | `_currentMatchIndex = -1`、`MatchCountText = "0/0"` |
| 重算前選中那筆已不在新序列 | `IndexOf` 回 -1 → fallback 到 index 0 |
| `OnSortChanged` 短時間連發 | 每次都重算，邏輯冪等；不做 throttle |
| `OnDetachedFromVisualTree` 後 provider 仍被持有 | View 在 detach 時把 `OrderedProductsProvider` 設回 `null`，避免持參考 leak |

## 測試策略

### VM 單元測試（新增 `tests/Honeycomb.Tests/ProductSearchNavigationTests.cs`）

延用現有 xUnit + InMemory SQLite 架構。透過手動設定 `OrderedProductsProvider` 模擬「View 提供的順序」，**不需要真的拉起 DataGrid**。

1. **`RecomputeMatches_UsesOrderedProvider_WhenProvided`**
   - 準備：Products = [b, a, c]，provider 回傳 [a, b, c]
   - 執行：搜尋全配的 query
   - 斷言：`_matches` 順序 = [a, b, c]，不是 [b, a, c]

2. **`NextMatch_FollowsProviderOrder`**
   - 準備：provider 回傳 [a, b, c]，全部 match
   - 執行：`NextMatch()` ×2
   - 斷言：`MatchCountText = "3/3"`，目前選中 = c

3. **`OnSortChanged_PreservesSelectedItem`**
   - 準備：provider 回傳 [a, b, c]，搜尋後選中 b（`_currentMatchIndex = 1`）
   - 執行：把 provider 改回傳 [c, b, a]，呼叫 `OnSortChanged()`
   - 斷言：`_currentMatchIndex = 1`（b 在新順序也是 index 1）

4. **`OnSortChanged_FallsBackToZero_WhenSelectedItemMissing`**
   - 準備：選中 b，把 b 從 provider 序列移除
   - 執行：`OnSortChanged()`
   - 斷言：`_currentMatchIndex = 0`

5. **`OnSortChanged_DoesNothing_WhenNoMatches`**
   - 準備：`SearchQuery = ""` 或無命中
   - 執行：`OnSortChanged()`
   - 斷言：`MatchCountText` 仍為 `"0/0"`，無 `MatchScrollRequested` 觸發

6. **`RecomputeMatches_FallsBackToProducts_WhenProviderNull`**
   - 準備：provider = null
   - 執行：搜尋
   - 斷言：`_matches` 順序 = `Products` 的順序，不爆 NRE

每個測試另外驗證 `MatchScrollRequested` 觸發次數，確認情境 3「不在切排序時 scroll」的契約有守住。

### 手動驗證腳本

1. 啟動 app → 第一欄「商品名稱」自動 Ascending
2. Ctrl+F → 輸入 `test` → 確認 ↓ 走的是商品名稱字母順序
3. 點「上架價格」欄 header → 切成升序 → 觀察：
   - ✓ 原本選中那筆仍被選中（背景色保留）
   - ✓ `MatchCountText` 的 N 不變、目前 index 對應到新位置
   - ✓ 沒有強制 scroll（畫面留在原位）
4. 按 ↓ → 確認跳到上架價格升序的下一筆 match
5. 再點同欄 header → 切降序 → 重複驗證
6. Esc 關搜尋 → 重開 → 索引重置正常

### 不寫 UI 整合測試的理由

現有 test 架構沒有 Avalonia headless 的設定，要為這個 fix 拉起一份 UI test infra 不划算。`Sorting` 事件 → `OnSortChanged` 的橋接是約 6 行 code-behind，靠手動驗證 + VM 單元測試已足夠覆蓋風險。

## Out of Scope

- 編輯「商品名稱」造成 match 集合變動 → 不即時更新（屬於另一個議題）
- 多欄排序支援（同桌已確認單欄就夠）
- DataGrid 完全沒套排序的狀態 → 走 fallback（`OrderedProductsProvider` 仍會回 `CollectionView`，只是沒套 sort，順序等同 source）
- 跨分類 Tab 切換時的搜尋狀態（沿用現有 `LoadData` 重置邏輯）
