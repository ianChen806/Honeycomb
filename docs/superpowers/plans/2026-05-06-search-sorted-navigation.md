# 商品搜尋導航跟隨 DataGrid 排序 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓搜尋功能（Ctrl+F）的「下一筆 / 上一筆」導航順序跟 DataGrid 當前畫面排序一致；搜尋進行中切換排序時，保留目前選中的那一筆作為新位置。

**Architecture:** ViewModel 引入 `OrderedProductsProvider: Func<IEnumerable<Product>>?` 注入點，`RecomputeMatches()` 改用 provider 取序列；新增 `OnSortChanged()` 重算 matches 並透過 `IndexOf` 找回原本選中那筆。View 在 `OnAttachedToVisualTree` 設 provider（從 `DataGrid.CollectionView` 取已排序序列）並訂閱 `DataGrid.Sorting` 事件，detach 時清除避免 leak。

**Tech Stack:** .NET 10 / C# 13、Avalonia 11.3.12、CommunityToolkit.Mvvm、EF Core + SQLite、xUnit。

**Spec:** `docs/superpowers/specs/2026-05-06-search-sorted-navigation-design.md`

**File Structure:**
- Modify: `src/Honeycomb/ViewModels/ProductListViewModel.cs` — 加 `OrderedProductsProvider` 屬性、加 `OnSortChanged()` 方法、修 `RecomputeMatches()` 改取 provider 序列
- Modify: `src/Honeycomb/Views/ProductListView.axaml.cs` — `OnAttachedToVisualTree` 設 provider + 訂閱 `Sorting`、`OnDetachedFromVisualTree` 清除、新增 `GetOrderedProducts()` 與 `OnGridSorting`
- Create: `tests/Honeycomb.Tests/ViewModels/ProductSearchSortNavigationTests.cs` — 6 個 VM 單元測試

---

## Task 1: 引入 OrderedProductsProvider，RecomputeMatches 改走 provider

**Files:**
- Create: `tests/Honeycomb.Tests/ViewModels/ProductSearchSortNavigationTests.cs`
- Modify: `src/Honeycomb/ViewModels/ProductListViewModel.cs:71-72`（加屬性）、`src/Honeycomb/ViewModels/ProductListViewModel.cs:303-323`（改 RecomputeMatches）

- [ ] **Step 1: 建立測試檔，寫第一個失敗測試**

Create `tests/Honeycomb.Tests/ViewModels/ProductSearchSortNavigationTests.cs`:

```csharp
using Honeycomb.Data;
using Honeycomb.Models;
using Honeycomb.Services;
using Honeycomb.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb.Tests.ViewModels;

public class ProductSearchSortNavigationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public ProductSearchSortNavigationTests()
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

    private Product AddProduct(string name, int categoryId = 1)
    {
        var currency = _db.Currencies.First();
        var product = new Product
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
        };
        _db.Products.Add(product);
        _db.SaveChanges();
        return product;
    }

    [Fact]
    public void RecomputeMatches_UsesOrderedProvider_WhenProvided()
    {
        AddProduct("Widget B");
        AddProduct("Widget A");
        AddProduct("Widget C");
        var vm = CreateVm(1);

        // 模擬 View 提供「按 Name 升序」順序
        var ordered = vm.Products.OrderBy(p => p.Name).ToList();
        vm.OrderedProductsProvider = () => ordered;

        Product? firstMatch = null;
        vm.MatchScrollRequested += p => firstMatch ??= p;

        vm.SearchQuery = "Widget";

        Assert.Equal("1/3", vm.MatchCountText);
        Assert.NotNull(firstMatch);
        Assert.Equal("Widget A", firstMatch!.Name);
    }
}
```

- [ ] **Step 2: 跑測試，預期 compile fail**

Run:
```bash
dotnet test --filter "FullyQualifiedName~ProductSearchSortNavigationTests"
```

Expected: build error CS1061 / CS0117 — `OrderedProductsProvider` 屬性不存在於 `ProductListViewModel`。

- [ ] **Step 3: 在 VM 加 `OrderedProductsProvider` 屬性**

Open `src/Honeycomb/ViewModels/ProductListViewModel.cs`, locate line 71-72:

```csharp
private List<Product> _matches = [];
private int _currentMatchIndex = -1;
```

Append immediately after line 72:

```csharp
public Func<IEnumerable<Product>>? OrderedProductsProvider { get; set; }
```

- [ ] **Step 4: 修改 `RecomputeMatches()` 走 provider，fallback 到 Products**

In the same file, locate `RecomputeMatches()` (line 303-323). Replace the matching computation block:

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

    var source = OrderedProductsProvider?.Invoke() ?? Products;
    _matches = source
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

（唯一的差別是第 9 行多了 `var source = OrderedProductsProvider?.Invoke() ?? Products;` 並把第 10 行的 `Products` 改成 `source`。）

- [ ] **Step 5: 跑測試，預期 PASS**

Run:
```bash
dotnet test --filter "FullyQualifiedName~ProductSearchSortNavigationTests.RecomputeMatches_UsesOrderedProvider_WhenProvided"
```

Expected: `Passed!  - Failed: 0, Passed: 1`

- [ ] **Step 6: 補 NextMatch 走 provider 順序的測試**

Append to `ProductSearchSortNavigationTests.cs`:

```csharp
    [Fact]
    public void NextMatch_FollowsProviderOrder()
    {
        AddProduct("Widget B");
        AddProduct("Widget A");
        AddProduct("Widget C");
        var vm = CreateVm(1);

        var ordered = vm.Products.OrderBy(p => p.Name).ToList(); // A, B, C
        vm.OrderedProductsProvider = () => ordered;

        vm.SearchQuery = "Widget"; // 1/3, 選中 A

        Product? scrolled = null;
        vm.MatchScrollRequested += p => scrolled = p;

        vm.NextMatch();
        Assert.Equal("2/3", vm.MatchCountText);
        Assert.Equal("Widget B", scrolled!.Name);

        vm.NextMatch();
        Assert.Equal("3/3", vm.MatchCountText);
        Assert.Equal("Widget C", scrolled!.Name);
    }
```

- [ ] **Step 7: 補 provider null fallback 測試**

Append:

```csharp
    [Fact]
    public void RecomputeMatches_FallsBackToProducts_WhenProviderNull()
    {
        AddProduct("Widget A");
        AddProduct("Widget B");
        var vm = CreateVm(1);
        // 不設 OrderedProductsProvider，預期走 Products fallback

        vm.SearchQuery = "Widget";

        Assert.Equal("1/2", vm.MatchCountText);
    }
```

- [ ] **Step 8: 跑全部新測試，預期 PASS**

Run:
```bash
dotnet test --filter "FullyQualifiedName~ProductSearchSortNavigationTests"
```

Expected: `Passed!  - Failed: 0, Passed: 3`

- [ ] **Step 9: 跑所有既有測試，確認沒打破其他東西**

Run:
```bash
dotnet test
```

Expected: `Passed!  - Failed: 0`（全部既有 ProductListSearchTests 等通過，因為 fallback 行為等於原本邏輯）

- [ ] **Step 10: Commit**

```bash
git add src/Honeycomb/ViewModels/ProductListViewModel.cs tests/Honeycomb.Tests/ViewModels/ProductSearchSortNavigationTests.cs
git commit -m "feat: 商品搜尋 RecomputeMatches 接受 OrderedProductsProvider 注入"
```

---

## Task 2: 加 OnSortChanged，搜尋中切排序時保留選中那筆

**Files:**
- Modify: `src/Honeycomb/ViewModels/ProductListViewModel.cs`（在 `RecomputeMatches` 之後加 `OnSortChanged`）
- Modify: `tests/Honeycomb.Tests/ViewModels/ProductSearchSortNavigationTests.cs`（補 4 個測試）

- [ ] **Step 1: 寫失敗測試 `OnSortChanged_PreservesSelectedItem`**

Append to `ProductSearchSortNavigationTests.cs`:

```csharp
    [Fact]
    public void OnSortChanged_PreservesSelectedItem()
    {
        AddProduct("Widget A");
        AddProduct("Widget B");
        AddProduct("Widget C");
        var vm = CreateVm(1);

        var asc = vm.Products.OrderBy(p => p.Name).ToList();         // A, B, C
        var desc = vm.Products.OrderByDescending(p => p.Name).ToList(); // C, B, A

        var ordered = asc;
        vm.OrderedProductsProvider = () => ordered;

        vm.SearchQuery = "Widget"; // 1/3, 選 A
        vm.NextMatch();            // 2/3, 選 B
        Assert.Equal("2/3", vm.MatchCountText);

        // 切到降序 ordering
        ordered = desc;
        vm.OnSortChanged();

        // B 在 desc 順序 [C, B, A] 中是 index 1，所以仍是 2/3
        Assert.Equal("2/3", vm.MatchCountText);
    }
```

- [ ] **Step 2: 跑測試，預期 compile fail**

Run:
```bash
dotnet test --filter "FullyQualifiedName~ProductSearchSortNavigationTests.OnSortChanged_PreservesSelectedItem"
```

Expected: build error — `OnSortChanged` 方法不存在。

- [ ] **Step 3: 在 VM 實作 `OnSortChanged()`**

Open `src/Honeycomb/ViewModels/ProductListViewModel.cs`. Locate `RecomputeMatches()` (the method updated in Task 1, now ending around line 326). Insert a new method **immediately after** `RecomputeMatches()`:

```csharp
public void OnSortChanged()
{
    if (_matches.Count == 0) return;

    var prevSelected = _currentMatchIndex >= 0 ? _matches[_currentMatchIndex] : null;

    var source = OrderedProductsProvider?.Invoke() ?? Products;
    _matches = source
        .Where(p => p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
        .ToList();

    if (_matches.Count == 0)
    {
        _currentMatchIndex = -1;
    }
    else
    {
        var newIndex = prevSelected is null ? 0 : _matches.IndexOf(prevSelected);
        _currentMatchIndex = newIndex >= 0 ? newIndex : 0;
    }

    UpdateMatchCountText();
    // 不觸發 MatchScrollRequested - 使用者剛在動排序，避免畫面打架
}
```

- [ ] **Step 4: 跑測試，預期 PASS**

Run:
```bash
dotnet test --filter "FullyQualifiedName~ProductSearchSortNavigationTests.OnSortChanged_PreservesSelectedItem"
```

Expected: `Passed!  - Failed: 0, Passed: 1`

- [ ] **Step 5: 補 fallback 測試 `OnSortChanged_FallsBackToZero_WhenSelectedItemMissing`**

Append:

```csharp
    [Fact]
    public void OnSortChanged_FallsBackToZero_WhenSelectedItemMissing()
    {
        var a = AddProduct("Widget A");
        var b = AddProduct("Widget B");
        var c = AddProduct("Widget C");
        var vm = CreateVm(1);

        var ordered = new List<Product> { a, b, c };
        vm.OrderedProductsProvider = () => ordered;

        vm.SearchQuery = "Widget"; // 1/3, 選 a
        vm.NextMatch();            // 2/3, 選 b

        // 模擬 b 從新 provider 序列消失（被刪 / 改名後不再 match 等情境）
        ordered = new List<Product> { a, c };
        vm.OnSortChanged();

        // b 不存在於新序列 → fallback 到 index 0 → "1/2"
        Assert.Equal("1/2", vm.MatchCountText);
    }
```

- [ ] **Step 6: 補早退測試 `OnSortChanged_DoesNothing_WhenNoMatches`**

Append:

```csharp
    [Fact]
    public void OnSortChanged_DoesNothing_WhenNoMatches()
    {
        AddProduct("Widget A");
        var vm = CreateVm(1);
        vm.OrderedProductsProvider = () => vm.Products;

        // 沒有設 SearchQuery → _matches 為空
        Product? scrolled = null;
        vm.MatchScrollRequested += p => scrolled = p;

        vm.OnSortChanged();

        Assert.Equal("0/0", vm.MatchCountText);
        Assert.Null(scrolled);
    }
```

- [ ] **Step 7: 補「切排序時不觸發 scroll」測試 `OnSortChanged_DoesNotRaiseScrollRequested`**

Append:

```csharp
    [Fact]
    public void OnSortChanged_DoesNotRaiseScrollRequested()
    {
        AddProduct("Widget A");
        AddProduct("Widget B");
        var vm = CreateVm(1);

        var ordered = vm.Products.ToList();
        vm.OrderedProductsProvider = () => ordered;

        vm.SearchQuery = "Widget"; // 此時會 fire 一次 scroll，但我們在這之後才 subscribe

        int scrollCount = 0;
        vm.MatchScrollRequested += _ => scrollCount++;

        vm.OnSortChanged();

        Assert.Equal(0, scrollCount);
    }
```

- [ ] **Step 8: 跑所有新測試，預期 6 個全 PASS**

Run:
```bash
dotnet test --filter "FullyQualifiedName~ProductSearchSortNavigationTests"
```

Expected: `Passed!  - Failed: 0, Passed: 6`

- [ ] **Step 9: 跑所有測試，預期沒回歸**

Run:
```bash
dotnet test
```

Expected: 全綠。

- [ ] **Step 10: Commit**

```bash
git add src/Honeycomb/ViewModels/ProductListViewModel.cs tests/Honeycomb.Tests/ViewModels/ProductSearchSortNavigationTests.cs
git commit -m "feat: 商品搜尋切排序時保留目前選中那筆 (OnSortChanged)"
```

---

## Task 3: View 端串接 DataGrid Sorting → VM

**Files:**
- Modify: `src/Honeycomb/Views/ProductListView.axaml.cs`

這部分 logic 仰賴 Avalonia DataGrid 的 runtime 行為，靠 unit test 無法直接驗證 → 用 build pass + Task 4 手動腳本確認。

- [ ] **Step 1: 加新 using**

Open `src/Honeycomb/Views/ProductListView.axaml.cs`. At the top of the file, locate the existing usings (line 1-11) and add these two if they're not already there:

```csharp
using Avalonia.Collections;
using Avalonia.Threading;
```

完整 usings 區塊（已存在的不重複）：

```csharp
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Honeycomb.Models;
using Honeycomb.Services;
using Honeycomb.ViewModels;
```

- [ ] **Step 2: 在 `OnAttachedToVisualTree` 補 provider 設定 + Sorting 訂閱**

Locate the existing `OnAttachedToVisualTree` (line 33-41) and replace it:

```csharp
protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
{
    base.OnAttachedToVisualTree(e);
    if (DataContext is ProductListViewModel vm)
    {
        vm.MatchScrollRequested += OnMatchScrollRequested;
        vm.PropertyChanged += OnVmPropertyChanged;
        vm.OrderedProductsProvider = GetOrderedProducts;
        ProductGrid.Sorting += OnGridSorting;
    }
}
```

- [ ] **Step 3: 在 `OnDetachedFromVisualTree` 補清除**

Locate the existing `OnDetachedFromVisualTree` (line 43-51) and replace it:

```csharp
protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
{
    if (DataContext is ProductListViewModel vm)
    {
        vm.MatchScrollRequested -= OnMatchScrollRequested;
        vm.PropertyChanged -= OnVmPropertyChanged;
        vm.OrderedProductsProvider = null;
        ProductGrid.Sorting -= OnGridSorting;
    }
    base.OnDetachedFromVisualTree(e);
}
```

- [ ] **Step 4: 加 `GetOrderedProducts` 方法**

In the same file, add a new private method anywhere among the helpers (suggested: 緊接在 `RestoreColumnWidths` 之前或 `OnMatchScrollRequested` 之後):

```csharp
private IEnumerable<Product> GetOrderedProducts()
{
    if (ProductGrid.CollectionView is { } view)
    {
        return view.OfType<Product>().ToList();
    }
    return DataContext is ProductListViewModel vm
        ? vm.Products.ToList()
        : new List<Product>();
}
```

說明：
- `ProductGrid.CollectionView` 型別是 `Avalonia.Collections.IDataGridCollectionView`，繼承 `IEnumerable`，反映 DataGrid 當前排序+篩選後的順序
- `OfType<Product>()` 比 `Cast<Product>()` 安全（遇到非 Product 元素不丟）
- `.ToList()` snapshot 當下順序，避免 enumerate 期間 collection changed

- [ ] **Step 5: 加 `OnGridSorting` handler**

Add immediately below `GetOrderedProducts`:

```csharp
private void OnGridSorting(object? sender, DataGridColumnEventArgs e)
{
    if (DataContext is not ProductListViewModel vm) return;
    // Sorting 事件在 DataGrid 即將套用排序前觸發。
    // 用 Post 排到下一個 UI tick，等內部排序套用完成再讓 VM 重算 matches。
    Dispatcher.UIThread.Post(vm.OnSortChanged);
}
```

- [ ] **Step 6: Build，確認編譯通過**

Run:
```bash
dotnet build Honeycomb.slnx
```

Expected: `Build succeeded.` 0 Error。

- [ ] **Step 7: 跑全部測試，預期沒回歸**

Run:
```bash
dotnet test
```

Expected: 全綠。Task 1+2 的 6 個新測試 + 既有 ProductListSearchTests 等都通過。

- [ ] **Step 8: Commit**

```bash
git add src/Honeycomb/Views/ProductListView.axaml.cs
git commit -m "feat: 搜尋導航跟隨 DataGrid Sorting 事件重算 matches"
```

---

## Task 4: 手動驗證

**Files:** —（無新改動，純驗證）

DataGrid 排序與搜尋導航的整合行為仰賴 UI runtime，本 task 用人類眼睛 + 鍵盤確認 spec 中的 6 步驟。

- [ ] **Step 1: 啟動 app**

```bash
dotnet run --project src/Honeycomb
```

Expected: app 視窗開啟，第一欄「商品名稱」自動套上升序排序箭頭。

- [ ] **Step 2: 開搜尋並輸入 query**

按 `Ctrl+F` → 浮動搜尋層出現 → 輸入存在於商品名稱中的字串（例如某個現有商品的字根）。

Expected: 搜尋層右側顯示 `1/N`（N 為命中數），DataGrid 上第一筆 match 變成選中狀態並滾動到視野。

- [ ] **Step 3: 確認 ↓ 導航走商品名稱字母順序**

按 `Enter` 兩次 → 觀察選中項目。

Expected: 選中那筆按商品名稱字母升序往下跳；`MatchCountText` 顯示 `2/N`、`3/N`。

- [ ] **Step 4: 切到「上架價格」欄升序**

Click 商品名稱以外的欄位 header（例如「上架價格」）。

Expected:
- ✓ 原本選中那筆**仍被選中**（背景色保留、程式邏輯有保留 reference）
- ✓ `MatchCountText` 的 N 不變
- ✓ 目前 index 對應到「該筆在新排序的位置」（例如原本 `2/N` 切換後可能變 `5/N`）
- ✓ DataGrid **沒有強制 scroll** —— 畫面留在使用者剛剛點 header 的位置

- [ ] **Step 5: 按 ↓ 確認跳到上架價格升序的下一筆 match**

按 `Enter`。

Expected: 跳到上架價格升序中的下一筆命中項目，**不是**商品名稱字母順序的下一筆。

- [ ] **Step 6: 切降序、再驗證**

再 click 一次同欄 header（切降序） → 重複 Step 4 + Step 5 的驗證。

Expected: 同樣保留選中、index 重算、↓ 走降序順序。

- [ ] **Step 7: 收尾**

按 `Esc` 關搜尋 → 重開 → 確認索引重置為 `1/N`（沿用既有 `LoadData` / `OpenSearch` 行為，不該被本次改動破壞）。

如果以上所有步驟都符合預期 → Task 4 完成，整個實作 Done。
若任一步驟異常，回到對應 Task 的測試補測試 case，再修 implementation。
