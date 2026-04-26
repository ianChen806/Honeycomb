> Seeded from plan: docs/superpowers/plans/2026-04-26-search-ctrlf.md

## Approach

# 商品搜尋功能（Ctrl+F）實作計畫

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `ProductListView` 加上 Ctrl+F 商品名稱搜尋功能，支援高亮匹配列、Enter/Shift+Enter 導航、X/Y 計數顯示。

**Architecture:** 搜尋狀態與匹配邏輯放在 `ProductListViewModel`；高亮機制借用 DataGrid 內建的 `SelectedItem` + `ScrollIntoView`；UI 為 `ProductListView` 右上角浮動 Border，由 `IsSearchVisible` 控制顯示。

**Tech Stack:** .NET 10、Avalonia 11、CommunityToolkit.Mvvm、xUnit + EF Core SQLite in-memory（測試）。

**Spec:** `docs/superpowers/specs/2026-04-26-search-ctrlf-design.md`

---

## File Structure

**Modify:**
- `src/Honeycomb/ViewModels/ProductListViewModel.cs` — 加入搜尋狀態、匹配與導航邏輯、`OpenSearch`/`CloseSearch`、`MatchScrollRequested` 事件、`LoadData` 內呼叫 `ResetSearch`
- `src/Honeycomb/Views/ProductListView.axaml` — 加入浮動搜尋 Border、`UserControl.KeyBindings` 中 Ctrl+F 綁定
- `src/Honeycomb/Views/ProductListView.axaml.cs` — 新增 `OnSearchKeyDown`、`OnCloseSearchClicked`、訂閱/解訂 `MatchScrollRequested`、`OpenSearch` 後將焦點移至 SearchBox

**Create:**
- `tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs` — 9 個搜尋邏輯測試

---

## Task 1: 搜尋狀態與匹配邏輯

加入 `SearchQuery`、`MatchCountText`、內部 `_matches` 與 `_currentMatchIndex`。query 變更時重算 matches、重置索引。

**Files:**
- Create: `tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs`
- Modify: `src/Honeycomb/ViewModels/ProductListViewModel.cs`

- [ ] **Step 1: 建立測試檔，寫前 4 個 failing test**

建立 `tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs`：

```csharp
using Honeycomb.Data;
using Honeycomb.Models;
using Honeycomb.Services;
using Honeycomb.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb.Tests.ViewModels;

public class ProductListSearchTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public ProductListSearchTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _db.Currencies.Add(new Currency { Code = "TWD", Name = "新台幣" });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private ProductListViewModel CreateVm(int categoryId = 1)
    {
        return new ProductListViewModel(
            _db,
            new ExcelExportService(),
            () => Task.FromResult<string?>(null),
            categoryId);
    }

    private void AddProduct(string name, int categoryId = 1)
    {
        var currency = _db.Currencies.First();
        _db.Products.Add(new Product
        {
            Name = name,
            UnitPrice = 100,
            CurrencyId = currency.Id,
            ExchangeRate = 1,
            Discount = 1,
            ListingPrice = 200,
            CommissionFee = 10,
            CategoryId = categoryId,
            CreatedAt = DateTime.Now
        });
        _db.SaveChanges();
    }

    [Fact]
    public void EmptyQuery_ClearsMatchCount()
    {
        AddProduct("Widget A");
        var vm = CreateVm(1);

        vm.SearchQuery = "";

        Assert.Equal("0/0", vm.MatchCountText);
    }

    [Fact]
    public void NoMatch_SetsCountToZero()
    {
        AddProduct("Widget A");
        var vm = CreateVm(1);

        vm.SearchQuery = "zzz";

        Assert.Equal("0/0", vm.MatchCountText);
    }

    [Fact]
    public void SingleMatch_SetsCountToOneOfOne()
    {
        AddProduct("Widget A");
        AddProduct("Other");
        var vm = CreateVm(1);

        vm.SearchQuery = "Widget";

        Assert.Equal("1/1", vm.MatchCountText);
    }

    [Fact]
    public void MultipleMatches_OrderedByProductsCollection()
    {
        AddProduct("Widget A");
        AddProduct("Widget B");
        AddProduct("Other");
        var vm = CreateVm(1);

        vm.SearchQuery = "Widget";

        Assert.Equal("1/2", vm.MatchCountText);
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

```bash
dotnet test --filter "FullyQualifiedName~ProductListSearchTests"
```

Expected: 編譯錯誤（`SearchQuery`、`MatchCountText` 屬性不存在）

- [ ] **Step 3: 在 ProductListViewModel 加入搜尋狀態**

修改 `src/Honeycomb/ViewModels/ProductListViewModel.cs`，在類別欄位區（其他 `[ObservableProperty]` 之間）加入：

```csharp
[ObservableProperty]
private string _searchQuery = string.Empty;

[ObservableProperty]
private bool _isSearchVisible;

[ObservableProperty]
private string _matchCountText = "0/0";

private List<Product> _matches = [];
private int _currentMatchIndex = -1;
```

並在 class 末尾加入 partial method 與更新邏輯：

```csharp
partial void OnSearchQueryChanged(string value)
{
    RecomputeMatches();
}

private void RecomputeMatches()
{
    if (string.IsNullOrEmpty(SearchQuery))
    {
        _matches = [];
        _currentMatchIndex = -1;
        MatchCountText = "0/0";
        return;
    }

    _matches = Products
        .Where(p => p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
        .ToList();

    _currentMatchIndex = _matches.Count > 0 ? 0 : -1;
    UpdateMatchCountText();
}

private void UpdateMatchCountText()
{
    if (_matches.Count == 0)
    {
        MatchCountText = "0/0";
    }
    else
    {
        MatchCountText = $"{_currentMatchIndex + 1}/{_matches.Count}";
    }
}
```

頂端 using 確認有 `System`、`System.Collections.Generic`、`System.Linq`（已存在）。

- [ ] **Step 4: 跑測試確認通過**

```bash
dotnet test --filter "FullyQualifiedName~ProductListSearchTests"
```

Expected: 4 passed

- [ ] **Step 5: Commit**

```bash
git add src/Honeycomb/ViewModels/ProductListViewModel.cs tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs
git commit -m "feat: 商品搜尋狀態與匹配邏輯"
```

---

## Task 2: 導航（Next / Previous 含 wrap-around）

加入 `NextMatch()`、`PreviousMatch()`、`MatchScrollRequested` 事件。事件提供 View 端執行 `ScrollIntoView` 的鉤子。

**Files:**
- Modify: `src/Honeycomb/ViewModels/ProductListViewModel.cs`
- Modify: `tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs`

- [ ] **Step 1: 新增 4 個 failing test**

在 `ProductListSearchTests` class 末尾追加：

```csharp
[Fact]
public void NextMatch_AdvancesIndex()
{
    AddProduct("Widget A");
    AddProduct("Widget B");
    AddProduct("Widget C");
    var vm = CreateVm(1);
    vm.SearchQuery = "Widget";
    Assert.Equal("1/3", vm.MatchCountText);

    vm.NextMatch();

    Assert.Equal("2/3", vm.MatchCountText);
}

[Fact]
public void NextMatch_WrapsAroundAtEnd()
{
    AddProduct("Widget A");
    AddProduct("Widget B");
    var vm = CreateVm(1);
    vm.SearchQuery = "Widget";

    vm.NextMatch();   // 2/2
    vm.NextMatch();   // wrap to 1/2

    Assert.Equal("1/2", vm.MatchCountText);
}

[Fact]
public void PreviousMatch_WrapsAroundAtStart()
{
    AddProduct("Widget A");
    AddProduct("Widget B");
    var vm = CreateVm(1);
    vm.SearchQuery = "Widget";   // 1/2

    vm.PreviousMatch();          // wrap to 2/2

    Assert.Equal("2/2", vm.MatchCountText);
}

[Fact]
public void NextMatch_RaisesMatchScrollRequested()
{
    AddProduct("Widget A");
    AddProduct("Widget B");
    var vm = CreateVm(1);
    vm.SearchQuery = "Widget";

    Product? scrolled = null;
    vm.MatchScrollRequested += p => scrolled = p;

    vm.NextMatch();

    Assert.NotNull(scrolled);
    Assert.Equal("Widget B", scrolled!.Name);
}
```

- [ ] **Step 2: 跑測試確認失敗**

```bash
dotnet test --filter "FullyQualifiedName~ProductListSearchTests"
```

Expected: 編譯錯誤（`NextMatch`、`PreviousMatch`、`MatchScrollRequested` 不存在）

- [ ] **Step 3: 加入事件、Next/Previous 方法、並修改 RecomputeMatches 也觸發 scroll**

在 `ProductListViewModel` 的欄位/事件區（建議放在現有 `ProductsMoved` 事件附近）加入：

```csharp
public event Action<Product>? MatchScrollRequested;
```

在 class 末尾加入：

```csharp
public void NextMatch()
{
    if (_matches.Count == 0) return;
    _currentMatchIndex = (_currentMatchIndex + 1) % _matches.Count;
    UpdateMatchCountText();
    MatchScrollRequested?.Invoke(_matches[_currentMatchIndex]);
}

public void PreviousMatch()
{
    if (_matches.Count == 0) return;
    _currentMatchIndex = (_currentMatchIndex - 1 + _matches.Count) % _matches.Count;
    UpdateMatchCountText();
    MatchScrollRequested?.Invoke(_matches[_currentMatchIndex]);
}
```

並修改 `RecomputeMatches`，讓有匹配時也觸發 scroll：

```csharp
private void RecomputeMatches()
{
    if (string.IsNullOrEmpty(SearchQuery))
    {
        _matches = [];
        _currentMatchIndex = -1;
        MatchCountText = "0/0";
        return;
    }

    _matches = Products
        .Where(p => p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
        .ToList();

    _currentMatchIndex = _matches.Count > 0 ? 0 : -1;
    UpdateMatchCountText();

    if (_currentMatchIndex >= 0)
    {
        MatchScrollRequested?.Invoke(_matches[_currentMatchIndex]);
    }
}
```

- [ ] **Step 4: 跑測試確認通過**

```bash
dotnet test --filter "FullyQualifiedName~ProductListSearchTests"
```

Expected: 8 passed

- [ ] **Step 5: Commit**

```bash
git add src/Honeycomb/ViewModels/ProductListViewModel.cs tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs
git commit -m "feat: 商品搜尋導航與匹配捲動事件"
```

---

## Task 3: 大小寫不敏感、LoadData 重置搜尋

驗證大小寫不敏感行為（已在 Task 1 邏輯涵蓋），並讓 `LoadData()` 清空搜尋。

**Files:**
- Modify: `src/Honeycomb/ViewModels/ProductListViewModel.cs`
- Modify: `tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs`

- [ ] **Step 1: 新增 2 個測試**

在 `ProductListSearchTests` class 末尾追加：

```csharp
[Fact]
public void CaseInsensitive_MatchesRegardlessOfCase()
{
    AddProduct("Widget A");
    var vm = CreateVm(1);

    vm.SearchQuery = "widget";

    Assert.Equal("1/1", vm.MatchCountText);
}

[Fact]
public void LoadData_ResetsSearchState()
{
    AddProduct("Widget A");
    var vm = CreateVm(1);
    vm.SearchQuery = "Widget";
    Assert.Equal("1/1", vm.MatchCountText);

    vm.LoadData();

    Assert.Equal(string.Empty, vm.SearchQuery);
    Assert.Equal("0/0", vm.MatchCountText);
}
```

- [ ] **Step 2: 跑測試確認結果**

```bash
dotnet test --filter "FullyQualifiedName~ProductListSearchTests"
```

Expected: `CaseInsensitive_MatchesRegardlessOfCase` PASS（已涵蓋）；`LoadData_ResetsSearchState` FAIL

- [ ] **Step 3: 修改 LoadData 重置搜尋**

修改 `ProductListViewModel.LoadData()`，在開頭（`_db.ChangeTracker.Clear();` 之前）加入：

```csharp
SearchQuery = string.Empty;
```

`OnSearchQueryChanged` 會自動觸發 `RecomputeMatches`，清空 matches 與計數。

- [ ] **Step 4: 跑測試確認通過**

```bash
dotnet test --filter "FullyQualifiedName~ProductListSearchTests"
```

Expected: 10 passed

- [ ] **Step 5: 跑全部測試確認沒打壞舊功能**

```bash
dotnet test
```

Expected: all green

- [ ] **Step 6: Commit**

```bash
git add src/Honeycomb/ViewModels/ProductListViewModel.cs tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs
git commit -m "feat: LoadData 重置商品搜尋狀態"
```

---

## Task 4: 開關搜尋層（OpenSearch / CloseSearch）

加入 `OpenSearchCommand` 與 `CloseSearch()`，控制 `IsSearchVisible`。

**Files:**
- Modify: `src/Honeycomb/ViewModels/ProductListViewModel.cs`
- Modify: `tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs`

- [ ] **Step 1: 新增 2 個測試**

在 `ProductListSearchTests` class 末尾追加：

```csharp
[Fact]
public void OpenSearchCommand_SetsIsSearchVisibleTrue()
{
    var vm = CreateVm(1);
    Assert.False(vm.IsSearchVisible);

    vm.OpenSearchCommand.Execute(null);

    Assert.True(vm.IsSearchVisible);
}

[Fact]
public void CloseSearch_SetsIsSearchVisibleFalse()
{
    var vm = CreateVm(1);
    vm.OpenSearchCommand.Execute(null);

    vm.CloseSearch();

    Assert.False(vm.IsSearchVisible);
}
```

- [ ] **Step 2: 跑測試確認失敗**

```bash
dotnet test --filter "FullyQualifiedName~ProductListSearchTests"
```

Expected: 編譯錯誤（`OpenSearchCommand`、`CloseSearch` 不存在）

- [ ] **Step 3: 加入命令與方法**

在 `ProductListViewModel` 加入（建議放在現有 `[RelayCommand] AddProduct` 附近）：

```csharp
[RelayCommand]
private void OpenSearch()
{
    IsSearchVisible = true;
}

public void CloseSearch()
{
    IsSearchVisible = false;
}
```

- [ ] **Step 4: 跑測試確認通過**

```bash
dotnet test --filter "FullyQualifiedName~ProductListSearchTests"
```

Expected: 12 passed

- [ ] **Step 5: Commit**

```bash
git add src/Honeycomb/ViewModels/ProductListViewModel.cs tests/Honeycomb.Tests/ViewModels/ProductListSearchTests.cs
git commit -m "feat: OpenSearch / CloseSearch 控制搜尋層顯示"
```

---

## Task 5: 加入浮動搜尋層 UI

在 `ProductListView.axaml` 加入浮動 Border、UserControl 層的 Ctrl+F KeyBinding。

**Files:**
- Modify: `src/Honeycomb/Views/ProductListView.axaml`

- [ ] **Step 1: 加入 UserControl.KeyBindings**

在 `src/Honeycomb/Views/ProductListView.axaml` 中,緊接 `x:DataType="vm:ProductListViewModel">` 結束標籤之後（即在 `<Grid ...>` 之前），加入：

```xaml
<UserControl.KeyBindings>
    <KeyBinding Gesture="Ctrl+F" Command="{Binding OpenSearchCommand}"/>
</UserControl.KeyBindings>
```

- [ ] **Step 2: 在 Grid Row 1（DataGrid 同格）加入浮動搜尋 Border**

在 `<DataGrid Grid.Row="1" ... >...</DataGrid>` 之後、`<!-- Bottom Bar -->` 之前，加入：

```xaml
<!-- Floating Search Overlay -->
<Border Grid.Row="1"
        IsVisible="{Binding IsSearchVisible}"
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

- [ ] **Step 3: Build 確認 XAML 沒有語法錯誤（先暫時加占位 handler）**

因為 `OnSearchKeyDown` 和 `OnCloseSearchClicked` 還沒在 code-behind 實作，build 會失敗。先在 code-behind 加暫時占位 handler 讓 build 過。

修改 `src/Honeycomb/Views/ProductListView.axaml.cs`，在 class 內加入：

```csharp
private void OnSearchKeyDown(object? sender, KeyEventArgs e) { }
private void OnCloseSearchClicked(object? sender, RoutedEventArgs e) { }
```

- [ ] **Step 4: Build 驗證**

```bash
dotnet build Honeycomb.slnx
```

Expected: build succeeded

- [ ] **Step 5: Commit**

```bash
git add src/Honeycomb/Views/ProductListView.axaml src/Honeycomb/Views/ProductListView.axaml.cs
git commit -m "feat: 商品搜尋浮動 UI 與 Ctrl+F 綁定"
```

---

## Task 6: 鍵盤事件與 ScrollIntoView 串接

實作 code-behind 的 `OnSearchKeyDown`、`OnCloseSearchClicked`、`MatchScrollRequested` 訂閱、`OpenSearch` 後焦點移至 SearchBox。

**Files:**
- Modify: `src/Honeycomb/Views/ProductListView.axaml.cs`

- [ ] **Step 1: 改寫 code-behind 加入完整邏輯**

修改 `src/Honeycomb/Views/ProductListView.axaml.cs`：

(a) 在 `OnLoaded` 之後加入 DataContext 訂閱／解訂邏輯。最乾淨的做法是覆寫 `OnAttachedToVisualTree` / `OnDetachedFromVisualTree`：

```csharp
protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnAttachedToVisualTree(e);
    if (DataContext is ProductListViewModel vm)
    {
        vm.MatchScrollRequested += OnMatchScrollRequested;
        vm.PropertyChanged += OnVmPropertyChanged;
    }
}

protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    if (DataContext is ProductListViewModel vm)
    {
        vm.MatchScrollRequested -= OnMatchScrollRequested;
        vm.PropertyChanged -= OnVmPropertyChanged;
    }
    base.OnDetachedFromVisualTree(e);
}

private void OnMatchScrollRequested(Product product)
{
    ProductGrid.SelectedItem = product;
    ProductGrid.ScrollIntoView(product, null);
}

private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(ProductListViewModel.IsSearchVisible)
        && sender is ProductListViewModel vm
        && vm.IsSearchVisible)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
        });
    }
}
```

(b) 取代 Step 5 的占位 handler 為完整實作：

```csharp
private void OnSearchKeyDown(object? sender, KeyEventArgs e)
{
    if (DataContext is not ProductListViewModel vm) return;

    switch (e.Key)
    {
        case Key.Enter:
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                vm.PreviousMatch();
            else
                vm.NextMatch();
            e.Handled = true;
            break;

        case Key.Escape:
            vm.CloseSearch();
            ProductGrid.Focus();
            e.Handled = true;
            break;
    }
}

private void OnCloseSearchClicked(object? sender, RoutedEventArgs e)
{
    if (DataContext is ProductListViewModel vm)
    {
        vm.CloseSearch();
        ProductGrid.Focus();
    }
}
```

確認 `using` 區段包含：
```csharp
using System.ComponentModel;       // PropertyChangedEventArgs（已存在）
using Avalonia.Input;              // Key、KeyEventArgs、KeyModifiers（已存在）
```

- [ ] **Step 2: Build 驗證**

```bash
dotnet build Honeycomb.slnx
```

Expected: build succeeded

- [ ] **Step 3: 跑全部測試**

```bash
dotnet test
```

Expected: all green

- [ ] **Step 4: 手動煙霧測試**

```bash
dotnet run --project src/Honeycomb
```

驗證以下行為（每項都要勾起來）：

- [ ] 在分類 Tab 內按 `Ctrl+F`：右上角浮現搜尋層，焦點落在 TextBox
- [ ] 輸入商品名稱片段：DataGrid 跳到第一筆匹配並選取，計數顯示 `1/N`
- [ ] 按 `Enter`：跳到下一筆匹配，計數遞增
- [ ] 在最後一筆按 `Enter`：wrap 回 `1/N`
- [ ] 按 `Shift+Enter`：跳到上一筆
- [ ] 在第一筆按 `Shift+Enter`：wrap 到最後一筆
- [ ] 大小寫不敏感（輸入小寫能找到大寫商品名）
- [ ] 按 `Esc`：搜尋層關閉，焦點回 DataGrid
- [ ] 再按 `Ctrl+F`：搜尋層重新浮現，TextBox 內保留上次的 query
- [ ] 點 ✕ 按鈕：等同 Esc 的關閉行為
- [ ] 切換到別的分類 Tab：原 Tab 的搜尋層不影響新 Tab；新 Tab 預設無搜尋層
- [ ] 新增 / 刪除 / 移動商品：搜尋自動清空（query 變空、計數歸零）

- [ ] **Step 5: Commit**

```bash
git add src/Honeycomb/Views/ProductListView.axaml.cs
git commit -m "feat: 商品搜尋鍵盤導航與捲動串接"
```

---

## 完成標準

- 全部測試 green（共 12 個新增搜尋測試）
- `dotnet build Honeycomb.slnx` 成功
- 手動煙霧測試 12 項全數通過
- 6 個 commit 對應 6 個 Task
