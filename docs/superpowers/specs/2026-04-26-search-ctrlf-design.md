# 商品搜尋功能（Ctrl+F）設計規格

**日期：** 2026-04-26
**範圍：** Honeycomb 庫存管理系統 — `ProductListView` 浮動搜尋層

## 目標

在每個分類 Tab 內加入 Ctrl+F 商品搜尋功能，讓使用者快速定位商品列。

## 規格決策摘要

| 項目 | 決策 |
|------|------|
| 搜尋範圍 | 僅當前 Tab |
| 搜尋欄位 | 僅商品名稱 |
| 顯示方式 | 高亮模式（不過濾，匹配列以 SelectedItem 呈現並滾動到視野） |
| UI 形式 | 浮動覆蓋層，預設隱藏 |
| 鍵盤操作 | Ctrl+F 開啟、Esc 關閉、Enter 跳下一個、Shift+Enter 跳上一個、顯示 X/Y 計數 |
| 高亮機制 | 借用 DataGrid 的 `SelectedItem` + `ScrollIntoView` |

## 架構

### 動到的檔案

- `src/Honeycomb/ViewModels/ProductListViewModel.cs` — 加入搜尋狀態與導航邏輯
- `src/Honeycomb/Views/ProductListView.axaml` — 加入浮動搜尋層 + KeyBindings
- `src/Honeycomb/Views/ProductListView.axaml.cs` — 處理鍵盤事件與 ScrollIntoView 串接
- `tests/Honeycomb.Tests/ProductListSearchTests.cs` — 新增

### ProductListViewModel 新增狀態

```csharp
[ObservableProperty] private string _searchQuery = string.Empty;
[ObservableProperty] private bool _isSearchVisible;
[ObservableProperty] private string _matchCountText = "0/0";

private List<Product> _matches = [];
private int _currentMatchIndex = -1;
```

### ProductListViewModel 新增方法

- `OpenSearch()` — 設定 `IsSearchVisible = true`
- `CloseSearch()` — 設定 `IsSearchVisible = false`
- `OnSearchQueryChanged(string)` — partial 方法，重算 `_matches`、跳到第一筆並觸發 `MatchScrollRequested`
- `NextMatch()` — `_currentMatchIndex` 前進、wrap、觸發 `MatchScrollRequested`
- `PreviousMatch()` — `_currentMatchIndex` 後退、wrap、觸發 `MatchScrollRequested`
- `ResetSearch()` — 清空 query 與索引（在 `LoadData()` 內呼叫）

### 事件

```csharp
public event Action<Product>? MatchScrollRequested;
```

ViewModel 不直接操作 DataGrid。View 訂閱此事件後執行：

```csharp
ProductGrid.SelectedItem = product;
ProductGrid.ScrollIntoView(product, null);
```

### 比對邏輯

```csharp
_matches = Products
    .Where(p => p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
    .ToList();
```

順序與 `Products` 集合一致（即 DataGrid 顯示順序）。

## UI

### 浮動搜尋層位置

放在 `ProductListView` 最外層 `Grid` 的 Row 1（DataGrid 同格），透過：

```xaml
HorizontalAlignment="Right"
VerticalAlignment="Top"
Margin="0,8,16,0"
```

定位於 DataGrid 右上角，疊加顯示。

### 搜尋層內容

```xaml
<Border IsVisible="{Binding IsSearchVisible}"
        HorizontalAlignment="Right" VerticalAlignment="Top"
        Margin="0,8,16,0" Padding="8" CornerRadius="6"
        Background="{DynamicResource SystemControlBackgroundChromeMediumLowBrush}"
        BoxShadow="0 2 8 0 #40000000">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <TextBox Name="SearchBox" Width="200" Watermark="搜尋商品名稱"
                 Text="{Binding SearchQuery}" KeyDown="OnSearchKeyDown"/>
        <TextBlock Text="{Binding MatchCountText}" VerticalAlignment="Center"
                   Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"/>
        <Button Content="✕" Click="OnCloseSearchClicked"/>
    </StackPanel>
</Border>
```

### 鍵盤事件處理（code-behind）

UserControl 層 `KeyBindings`：

```xaml
<UserControl.KeyBindings>
    <KeyBinding Gesture="Ctrl+F" Command="{Binding OpenSearchCommand}"/>
</UserControl.KeyBindings>
```

開啟後在 code-behind 將焦點移至 `SearchBox`。

`SearchBox` 的 `KeyDown` 事件處理：

| 按鍵 | 行為 |
|------|------|
| `Enter` | `vm.NextMatch()` |
| `Shift+Enter` | `vm.PreviousMatch()` |
| `Escape` | `vm.CloseSearch()` + 焦點還回 DataGrid |

## 邊界情境

| 情境 | 行為 |
|------|------|
| 空字串 query | `_matches=[]`、`MatchCountText="0/0"`、不動 SelectedItem |
| 沒有任何匹配 | `MatchCountText="0/0"`、不動 SelectedItem |
| 切換 Tab | 各 Tab 的 ViewModel 狀態獨立；當前 Tab 的 overlay 自然消失，回來時保留之前的 query |
| Enter 在最後一筆 | wrap 回第 0 筆 |
| Shift+Enter 在第 0 筆 | wrap 到最後一筆 |
| 大小寫 | 不分大小寫（`StringComparison.OrdinalIgnoreCase`） |
| query 變更時資料被改名 | 重新計算 matches（透過 `OnSearchQueryChanged` 觸發） |
| Esc 關閉後再 Ctrl+F 開啟 | 保留上次的 query 與選取 |
| 中途刪除 / 新增 / 移動商品 | `LoadData()` 內呼叫 `ResetSearch()` 清空 query |

## 測試

`tests/Honeycomb.Tests/ProductListSearchTests.cs` 涵蓋：

- `EmptyQuery_ClearsMatches`
- `NoMatch_SetsCountToZero`
- `SingleMatch_SetsCountAndSelectsFirst`
- `MultipleMatches_OrderedByProductsCollection`
- `NextMatch_AdvancesIndex`
- `NextMatch_WrapsAroundAtEnd`
- `PreviousMatch_WrapsAroundAtStart`
- `CaseInsensitive_MatchesRegardlessOfCase`
- `LoadData_ResetsSearchState`

`ScrollIntoView` 屬於 View 端純 UI 行為，不寫單元測試。

## 不在範圍內

- 跨分類搜尋
- 搜尋商品名稱以外的欄位（幣別、數字欄位等）
- 過濾模式
- 「不分大小寫」的可切換選項
- 正則表達式或進階比對語法
- 搜尋歷史紀錄
