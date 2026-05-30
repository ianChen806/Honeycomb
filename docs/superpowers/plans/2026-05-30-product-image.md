# 商品圖片功能 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 讓每件商品能掛一張代表圖，圖片以 JPEG 壓縮後存進 SQLite 的獨立 `ProductImage` 表（1 對 1），新增商品時與建立後皆可設定，並在清單旁的預覽面板顯示選中商品的圖。

**Architecture:** 圖片 BLOB 放在獨立 `ProductImage` 表（非 `Product` 欄位），讓清單/編輯查詢維持輕量、不把全部圖片位元組拉進記憶體；只在選中某列時按需查一筆。壓縮重用 Avalonia 已帶進的 SkiaSharp（解碼 → 等比縮放長邊上限 1024px、只縮不放 → 重新編碼 JPEG 品質 80）。ViewModel 只暴露 `byte[]?`，bytes→`Bitmap` 的轉換留在 View 層的 `IValueConverter`，讓 VM 維持可用現有純 xUnit + InMemory SQLite 測試。

**Tech Stack:** .NET 10、Avalonia 11.3.12（MVVM、StorageProvider 開檔）、EF Core 10 + SQLite、SkiaSharp 2.88.9、CommunityToolkit.Mvvm、xUnit。

**Spec:** `docs/superpowers/specs/2026-05-30-product-image-design.md`

---

## File Structure

| 動作 | 檔案 | 責任 |
|------|------|------|
| 新增 | `src/Honeycomb/Models/ProductImage.cs` | 1 對 1 圖片實體（`Id` / `ProductId` / `Data`） |
| 新增 | `src/Honeycomb/Services/ImageCompressionService.cs` | 無狀態壓縮：解碼→縮放→JPEG 編碼，純 `byte[]` 進出 |
| 新增 | `src/Honeycomb/Converters/BytesToBitmapConverter.cs` | View 層 `byte[]`→`Bitmap`（單向） |
| 新增 | `src/Honeycomb/Migrations/*_AddProductImage.cs` | EF 產生的建表 migration（含唯一索引 + cascade） |
| 修改 | `src/Honeycomb/Honeycomb.csproj` | 顯式 pin `SkiaSharp` 2.88.9 |
| 修改 | `src/Honeycomb/Data/AppDbContext.cs` | `DbSet<ProductImage>` + 1 對 1 關係設定 |
| 修改 | `src/Honeycomb/ViewModels/ProductListViewModel.cs` | 選取/圖片狀態與方法、`AddProduct` 收尾、建構子注入壓縮服務 |
| 修改 | `src/Honeycomb/ViewModels/MainWindowViewModel.cs` | 注入鏈：持有並傳遞 `ImageCompressionService` |
| 修改 | `src/Honeycomb/App.axaml.cs` | `new ImageCompressionService()` 並傳入 `MainWindowViewModel` |
| 修改 | `src/Honeycomb/Views/ProductListView.axaml` | 新增表單圖片區、清單列切左右欄、預覽面板 |
| 修改 | `src/Honeycomb/Views/ProductListView.axaml.cs` | 檔案選擇器事件（StorageProvider 開圖） |
| 新增 | `tests/Honeycomb.Tests/Data/ProductImageDbTests.cs` | schema：唯一索引、round-trip |
| 新增 | `tests/Honeycomb.Tests/ViewModels/ProductImageTests.cs` | VM：暫存/載入/upsert/移除/cascade |

> **不動 `Product.cs`。** 1 對 1 關係從 `ProductImage` 那側用 `HasOne<Product>().WithOne().HasForeignKey<ProductImage>(...)` 配置，`Product` 上不加 navigation property，確保 `LoadData()` 的清單查詢不可能誤帶圖片進記憶體。

> **測試連線字串：** 新增的兩個測試類使用 `"Data Source=:memory:;Foreign Keys=True"`。原因：cascade 刪除與 FK 完整性需要 SQLite 開啟 `foreign_keys` pragma；externally-opened 的 in-memory 連線不保證 EF 會代為開啟，明確帶 `Foreign Keys=True` 才能讓 cascade 測試可靠。（正式環境由 EF Core Sqlite provider 預設開啟 FK，cascade 正常。）

---

## Task 1: ProductImage 資料表與 1 對 1 關係

**Files:**
- Create: `src/Honeycomb/Models/ProductImage.cs`
- Modify: `src/Honeycomb/Data/AppDbContext.cs`
- Create: `src/Honeycomb/Migrations/*_AddProductImage.cs`（由 `dotnet ef` 產生）
- Test: `tests/Honeycomb.Tests/Data/ProductImageDbTests.cs`

- [ ] **Step 1: 寫失敗測試**

建立 `tests/Honeycomb.Tests/Data/ProductImageDbTests.cs`：

```csharp
using System;
using System.Linq;
using Honeycomb.Data;
using Honeycomb.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb.Tests.Data;

public class ProductImageDbTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public ProductImageDbTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
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

    private Product AddProduct(string name = "A")
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
            CategoryId = 1,
            CreatedAt = DateTime.Now
        };
        _db.Products.Add(product);
        _db.SaveChanges();
        return product;
    }

    [Fact]
    public void ProductImage_RoundTrips()
    {
        var product = AddProduct();
        var bytes = new byte[] { 1, 2, 3, 4 };

        _db.ProductImages.Add(new ProductImage { ProductId = product.Id, Data = bytes });
        _db.SaveChanges();

        var loaded = _db.ProductImages.Single(pi => pi.ProductId == product.Id);
        Assert.Equal(bytes, loaded.Data);
    }

    [Fact]
    public void ProductImage_DuplicateProductId_Throws()
    {
        var product = AddProduct();
        _db.ProductImages.Add(new ProductImage { ProductId = product.Id, Data = new byte[] { 1 } });
        _db.SaveChanges();

        _db.ProductImages.Add(new ProductImage { ProductId = product.Id, Data = new byte[] { 2 } });

        Assert.Throws<DbUpdateException>(() => _db.SaveChanges());
    }
}
```

- [ ] **Step 2: 跑測試確認失敗（編譯失敗即為 red）**

Run: `dotnet test --filter "FullyQualifiedName~ProductImageDbTests"`
Expected: 編譯失敗 — `ProductImage` 型別與 `_db.ProductImages` 不存在。

- [ ] **Step 3: 建立 `Models/ProductImage.cs`**

```csharp
namespace Honeycomb.Models;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public byte[] Data { get; set; } = [];
}
```

- [ ] **Step 4: 在 `AppDbContext` 加入 DbSet 與關係設定**

在 `src/Honeycomb/Data/AppDbContext.cs` 的 DbSet 區塊（現有 `public DbSet<Category> Categories => Set<Category>();` 之後）加一行：

```csharp
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
```

在 `OnModelCreating` 內、`modelBuilder.Entity<Product>(...)` 區塊**之後**加入：

```csharp
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(pi => pi.Id);
            entity.HasIndex(pi => pi.ProductId).IsUnique();
            entity.Property(pi => pi.Data).IsRequired();
            entity.HasOne<Product>()
                  .WithOne()
                  .HasForeignKey<ProductImage>(pi => pi.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
```

- [ ] **Step 5: 跑測試確認通過**

Run: `dotnet test --filter "FullyQualifiedName~ProductImageDbTests"`
Expected: PASS（2 passed）。

- [ ] **Step 6: 產生 EF migration**

Run: `dotnet ef migrations add AddProductImage --project src/Honeycomb/Honeycomb.csproj`
Expected: 在 `src/Honeycomb/Migrations/` 產生 `*_AddProductImage.cs`。打開確認 `Up()` 大致如下：

```csharp
migrationBuilder.CreateTable(
    name: "ProductImages",
    columns: table => new
    {
        Id = table.Column<int>(nullable: false)
            .Annotation("Sqlite:Autoincrement", true),
        ProductId = table.Column<int>(nullable: false),
        Data = table.Column<byte[]>(nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_ProductImages", x => x.Id);
        table.ForeignKey(
            name: "FK_ProductImages_Products_ProductId",
            column: x => x.ProductId,
            principalTable: "Products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    });

migrationBuilder.CreateIndex(
    name: "IX_ProductImages_ProductId",
    table: "ProductImages",
    column: "ProductId",
    unique: true);
```

若 `Up()` 沒有 `unique: true` 或不是 `Cascade`，回頭檢查 Step 4 的設定再 `dotnet ef migrations remove` 重產。

- [ ] **Step 7: 整體建置確認 migration 可編譯**

Run: `dotnet build Honeycomb.slnx`
Expected: Build succeeded（既有測試不受影響，建構子未變）。

- [ ] **Step 8: Commit**

```bash
git add src/Honeycomb/Models/ProductImage.cs \
        src/Honeycomb/Data/AppDbContext.cs \
        src/Honeycomb/Migrations/ \
        tests/Honeycomb.Tests/Data/ProductImageDbTests.cs
git commit -m "feat: 新增 ProductImage 資料表與 1 對 1 cascade 關係"
```

---

## Task 2: 圖片壓縮服務

**Files:**
- Modify: `src/Honeycomb/Honeycomb.csproj`
- Create: `src/Honeycomb/Services/ImageCompressionService.cs`
- Test: `tests/Honeycomb.Tests/Services/ImageCompressionServiceTests.cs`

- [ ] **Step 1: 顯式 pin SkiaSharp**

在 `src/Honeycomb/Honeycomb.csproj` 的套件 `ItemGroup` 內、`ClosedXML` 那行之後加入：

```xml
    <PackageReference Include="SkiaSharp" Version="2.88.9" />
```

（SkiaSharp 2.88.9 本就是 Avalonia 的傳遞依賴，連 Win32 原生資產一起帶進；顯式參考只是把它變成有意圖的直接依賴。）

- [ ] **Step 2: 寫失敗測試**

建立 `tests/Honeycomb.Tests/Services/ImageCompressionServiceTests.cs`：

```csharp
using System;
using System.IO;
using Honeycomb.Services;
using SkiaSharp;

namespace Honeycomb.Tests.Services;

public class ImageCompressionServiceTests
{
    private readonly ImageCompressionService _service = new();

    private static byte[] CreateImageBytes(int width, int height)
    {
        var info = new SKImageInfo(width, height);
        using var surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.CornflowerBlue);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static (int Width, int Height) Dimensions(byte[] bytes)
    {
        using var bmp = SKBitmap.Decode(bytes);
        return (bmp.Width, bmp.Height);
    }

    [Fact]
    public void Compress_DownscalesLargeImage_ToMaxEdge()
    {
        var input = CreateImageBytes(4000, 3000);

        var output = _service.Compress(new MemoryStream(input));

        var (w, h) = Dimensions(output);
        Assert.True(Math.Max(w, h) <= 1024, $"longest edge was {Math.Max(w, h)}");
        Assert.Equal(1024, w); // 4000 是長邊，縮到 1024
    }

    [Fact]
    public void Compress_DoesNotUpscaleSmallImage()
    {
        var input = CreateImageBytes(500, 400);

        var output = _service.Compress(new MemoryStream(input));

        var (w, h) = Dimensions(output);
        Assert.Equal(500, w);
        Assert.Equal(400, h);
    }

    [Fact]
    public void Compress_OutputIsJpeg()
    {
        var input = CreateImageBytes(800, 600);

        var output = _service.Compress(new MemoryStream(input));

        Assert.True(output.Length >= 3);
        Assert.Equal(0xFF, output[0]);
        Assert.Equal(0xD8, output[1]);
        Assert.Equal(0xFF, output[2]);
    }

    [Fact]
    public void Compress_InvalidBytes_Throws()
    {
        var garbage = new byte[] { 1, 2, 3, 4, 5 };

        Assert.Throws<InvalidOperationException>(
            () => _service.Compress(new MemoryStream(garbage)));
    }
}
```

- [ ] **Step 3: 跑測試確認失敗**

Run: `dotnet test --filter "FullyQualifiedName~ImageCompressionServiceTests"`
Expected: 編譯失敗 — `ImageCompressionService` 不存在。

- [ ] **Step 4: 實作 `Services/ImageCompressionService.cs`**

```csharp
using System;
using System.IO;
using SkiaSharp;

namespace Honeycomb.Services;

public class ImageCompressionService
{
    private const int MaxEdge = 1024;
    private const int JpegQuality = 80;

    /// <summary>解碼 → 等比縮放（只縮不放）→ 重新編碼為 JPEG。</summary>
    /// <exception cref="InvalidOperationException">來源無法解碼為圖片時。</exception>
    public byte[] Compress(Stream source)
    {
        using var original = SKBitmap.Decode(source);
        if (original is null)
            throw new InvalidOperationException("無法讀取圖片");

        var longestEdge = Math.Max(original.Width, original.Height);

        SKBitmap toEncode = original;
        SKBitmap? resized = null;
        if (longestEdge > MaxEdge)
        {
            var scale = (float)MaxEdge / longestEdge;
            var width = Math.Max(1, (int)Math.Round(original.Width * scale));
            var height = Math.Max(1, (int)Math.Round(original.Height * scale));
            resized = original.Resize(new SKImageInfo(width, height), SKFilterQuality.High)
                      ?? throw new InvalidOperationException("圖片縮放失敗");
            toEncode = resized;
        }

        try
        {
            using var image = SKImage.FromBitmap(toEncode);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
            return data.ToArray();
        }
        finally
        {
            resized?.Dispose();
        }
    }
}
```

- [ ] **Step 5: 跑測試確認通過**

Run: `dotnet test --filter "FullyQualifiedName~ImageCompressionServiceTests"`
Expected: PASS（4 passed）。

- [ ] **Step 6: Commit**

```bash
git add src/Honeycomb/Honeycomb.csproj \
        src/Honeycomb/Services/ImageCompressionService.cs \
        tests/Honeycomb.Tests/Services/ImageCompressionServiceTests.cs
git commit -m "feat: 加入 SkiaSharp 圖片壓縮服務"
```

---

## Task 3: ProductListViewModel 圖片邏輯與注入鏈

**Files:**
- Modify: `src/Honeycomb/ViewModels/ProductListViewModel.cs`
- Modify: `src/Honeycomb/ViewModels/MainWindowViewModel.cs:52`
- Modify: `src/Honeycomb/App.axaml.cs`
- Modify: `tests/Honeycomb.Tests/ViewModels/ProductListViewModelTests.cs:38-45`（既有 `CreateVm` helper）
- Test: `tests/Honeycomb.Tests/ViewModels/ProductImageTests.cs`

> 本任務變更 `ProductListViewModel` 建構子簽名，會牽動 `MainWindowViewModel`、`App.axaml.cs` 與既有測試的 `CreateVm`。Step 4 一次更新所有呼叫點，確保整個 solution 編譯。

- [ ] **Step 1: 寫失敗測試**

建立 `tests/Honeycomb.Tests/ViewModels/ProductImageTests.cs`：

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Honeycomb.Data;
using Honeycomb.Models;
using Honeycomb.Services;
using Honeycomb.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace Honeycomb.Tests.ViewModels;

public class ProductImageTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public ProductImageTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
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
        => new(_db, new ExcelExportService(), new ImageCompressionService(),
               () => Task.FromResult<string?>(null), categoryId);

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

    private static byte[] SampleImage(int width = 800, int height = 600)
    {
        var info = new SKImageInfo(width, height);
        using var surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.OrangeRed);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private void FillNewProductForm(ProductListViewModel vm, string name)
    {
        vm.NewName = name;
        vm.NewExtraCost = 0;
        vm.NewUnitPrice = 100;
        vm.NewCurrency = vm.Currencies.First();
        vm.NewExchangeRate = 1;
        vm.NewDiscount = 1;
        vm.NewListingPrice = 200;
        vm.NewCommissionFee = 10;
    }

    [Fact]
    public void SetNewImage_CompressesAndStages()
    {
        var vm = CreateVm();

        vm.SetNewImage(new MemoryStream(SampleImage()));

        Assert.NotNull(vm.NewImageBytes);
        Assert.NotEmpty(vm.NewImageBytes!);
    }

    [Fact]
    public void AddProduct_WithStagedImage_CreatesProductAndImageRow()
    {
        var vm = CreateVm();
        FillNewProductForm(vm, "有圖商品");
        vm.SetNewImage(new MemoryStream(SampleImage()));
        var staged = vm.NewImageBytes;

        vm.AddProductCommand.Execute(null);

        var product = _db.Products.Single(p => p.Name == "有圖商品");
        var image = _db.ProductImages.SingleOrDefault(pi => pi.ProductId == product.Id);
        Assert.NotNull(image);
        Assert.Equal(staged, image!.Data);
        Assert.Null(vm.NewImageBytes);
    }

    [Fact]
    public void AddProduct_WithoutImage_CreatesNoImageRow()
    {
        var vm = CreateVm();
        FillNewProductForm(vm, "無圖商品");

        vm.AddProductCommand.Execute(null);

        var product = _db.Products.Single(p => p.Name == "無圖商品");
        Assert.Empty(_db.ProductImages.Where(pi => pi.ProductId == product.Id));
    }

    [Fact]
    public void SelectedProduct_WithImage_LoadsBytes()
    {
        var p = AddProduct("A");
        var bytes = new ImageCompressionService().Compress(new MemoryStream(SampleImage()));
        _db.ProductImages.Add(new ProductImage { ProductId = p.Id, Data = bytes });
        _db.SaveChanges();

        var vm = CreateVm();
        vm.SelectedProduct = vm.Products.Single(x => x.Name == "A");

        Assert.Equal(bytes, vm.SelectedImageBytes);
    }

    [Fact]
    public void SelectedProduct_WithoutImage_ClearsBytes()
    {
        AddProduct("A");
        var vm = CreateVm();

        vm.SelectedProduct = vm.Products.Single(x => x.Name == "A");

        Assert.Null(vm.SelectedImageBytes);
    }

    [Fact]
    public void AttachImageToSelected_UpsertsSingleRow()
    {
        var p = AddProduct("A");
        var vm = CreateVm();
        vm.SelectedProduct = vm.Products.Single(x => x.Name == "A");

        vm.AttachImageToSelected(new MemoryStream(SampleImage()));
        Assert.NotNull(vm.SelectedImageBytes);
        Assert.Single(_db.ProductImages.Where(pi => pi.ProductId == p.Id));

        vm.AttachImageToSelected(new MemoryStream(SampleImage(400, 300)));
        Assert.Single(_db.ProductImages.Where(pi => pi.ProductId == p.Id));
    }

    [Fact]
    public void AttachImageToSelected_NoSelection_SetsError()
    {
        var vm = CreateVm();
        vm.SelectedProduct = null;

        vm.AttachImageToSelected(new MemoryStream(SampleImage()));

        Assert.False(string.IsNullOrEmpty(vm.ErrorMessage));
        Assert.Empty(_db.ProductImages);
    }

    [Fact]
    public void RemoveImageFromSelected_DeletesRow()
    {
        AddProduct("A");
        var vm = CreateVm();
        vm.SelectedProduct = vm.Products.Single(x => x.Name == "A");
        vm.AttachImageToSelected(new MemoryStream(SampleImage()));
        Assert.Single(_db.ProductImages);

        vm.RemoveImageFromSelected();

        Assert.Empty(_db.ProductImages);
        Assert.Null(vm.SelectedImageBytes);
    }

    [Fact]
    public void DeleteProduct_CascadeDeletesImage()
    {
        AddProduct("A");
        var vm = CreateVm();
        vm.SelectedProduct = vm.Products.Single(x => x.Name == "A");
        vm.AttachImageToSelected(new MemoryStream(SampleImage()));
        Assert.Single(_db.ProductImages);

        vm.DeleteProducts([vm.Products.Single(x => x.Name == "A")]);

        Assert.Empty(_db.ProductImages);
    }
}
```

- [ ] **Step 2: 跑測試確認失敗**

Run: `dotnet test --filter "FullyQualifiedName~ProductImageTests"`
Expected: 編譯失敗 — `ProductListViewModel` 沒有新建構子簽名，也沒有 `SetNewImage` / `AttachImageToSelected` / `RemoveImageFromSelected` / `SelectedProduct` / `SelectedImageBytes` / `NewImageBytes`。

- [ ] **Step 3: 改 `ProductListViewModel.cs` — 加 using、欄位、建構子、屬性、方法**

(a) 檔案最上方 using 區塊加入：

```csharp
using System.IO;
```

(b) 欄位區塊（現有 `private readonly ExcelExportService _excelExport;` 之下）加入：

```csharp
    private readonly ImageCompressionService _imageCompression;
```

(c) 建構子改簽名與指派。將：

```csharp
    public ProductListViewModel(AppDbContext db, ExcelExportService excelExport, Func<Task<string?>> getSaveFilePath, int categoryId = 1)
    {
        _db = db;
        _excelExport = excelExport;
        _getSaveFilePath = getSaveFilePath;
        _categoryId = categoryId;
        LoadData();
    }
```

改為：

```csharp
    public ProductListViewModel(AppDbContext db, ExcelExportService excelExport, ImageCompressionService imageCompression, Func<Task<string?>> getSaveFilePath, int categoryId = 1)
    {
        _db = db;
        _excelExport = excelExport;
        _imageCompression = imageCompression;
        _getSaveFilePath = getSaveFilePath;
        _categoryId = categoryId;
        LoadData();
    }
```

(d) 在建構子**之後**插入圖片相關成員：

```csharp
    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private byte[]? _selectedImageBytes;

    [ObservableProperty]
    private byte[]? _newImageBytes;

    public bool HasSelectedImage => SelectedImageBytes is not null;
    public bool HasNewImage => NewImageBytes is not null;

    partial void OnSelectedImageBytesChanged(byte[]? value) => OnPropertyChanged(nameof(HasSelectedImage));
    partial void OnNewImageBytesChanged(byte[]? value) => OnPropertyChanged(nameof(HasNewImage));

    partial void OnSelectedProductChanged(Product? value)
    {
        if (value is null)
        {
            SelectedImageBytes = null;
            return;
        }

        SelectedImageBytes = _db.ProductImages
            .Where(pi => pi.ProductId == value.Id)
            .Select(pi => pi.Data)
            .FirstOrDefault();
    }

    public void SetNewImage(Stream source)
    {
        ErrorMessage = string.Empty;
        try
        {
            NewImageBytes = _imageCompression.Compress(source);
        }
        catch (Exception)
        {
            NewImageBytes = null;
            ErrorMessage = "無法讀取圖片";
        }
    }

    public void AttachImageToSelected(Stream source)
    {
        ErrorMessage = string.Empty;

        if (SelectedProduct is null)
        {
            ErrorMessage = "請先選擇商品";
            return;
        }

        byte[] bytes;
        try
        {
            bytes = _imageCompression.Compress(source);
        }
        catch (Exception)
        {
            ErrorMessage = "無法讀取圖片";
            return;
        }

        var existing = _db.ProductImages.FirstOrDefault(pi => pi.ProductId == SelectedProduct.Id);
        if (existing is null)
            _db.ProductImages.Add(new ProductImage { ProductId = SelectedProduct.Id, Data = bytes });
        else
            existing.Data = bytes;

        _db.SaveChanges();
        SelectedImageBytes = bytes;
    }

    public void RemoveImageFromSelected()
    {
        ErrorMessage = string.Empty;

        if (SelectedProduct is null)
            return;

        var existing = _db.ProductImages.FirstOrDefault(pi => pi.ProductId == SelectedProduct.Id);
        if (existing is not null)
        {
            _db.ProductImages.Remove(existing);
            _db.SaveChanges();
        }

        SelectedImageBytes = null;
    }
```

(e) 修改現有 `AddProduct()`。把：

```csharp
        _db.Products.Add(product);
        _db.SaveChanges();

        NewName = string.Empty;
        NewExtraCost = 0;
        NewUnitPrice = null;
        NewListingPrice = null;

        LoadData();
```

改為：

```csharp
        _db.Products.Add(product);
        _db.SaveChanges();

        if (NewImageBytes is not null)
        {
            _db.ProductImages.Add(new ProductImage { ProductId = product.Id, Data = NewImageBytes });
            _db.SaveChanges();
        }

        NewName = string.Empty;
        NewExtraCost = 0;
        NewUnitPrice = null;
        NewListingPrice = null;
        NewImageBytes = null;

        LoadData();
```

- [ ] **Step 4: 更新所有建構子呼叫點**

(a) `src/Honeycomb/ViewModels/MainWindowViewModel.cs` — 加欄位、改建構子、改 line 52 的 `new`。

欄位區（現有 `private readonly ExcelExportService _excelExport;` 之下）加：

```csharp
    private readonly ImageCompressionService _imageCompression;
```

建構子簽名與指派。將：

```csharp
    public MainWindowViewModel(AppDbContext db, ExcelExportService excelExport, Func<Task<string?>> getSaveFilePath)
    {
        _db = db;
        _excelExport = excelExport;
        _getSaveFilePath = getSaveFilePath;
```

改為：

```csharp
    public MainWindowViewModel(AppDbContext db, ExcelExportService excelExport, ImageCompressionService imageCompression, Func<Task<string?>> getSaveFilePath)
    {
        _db = db;
        _excelExport = excelExport;
        _imageCompression = imageCompression;
        _getSaveFilePath = getSaveFilePath;
```

`RebuildCategoryTabs()` 內的建構。將：

```csharp
            var productList = new ProductListViewModel(_db, _excelExport, _getSaveFilePath, category.Id);
```

改為：

```csharp
            var productList = new ProductListViewModel(_db, _excelExport, _imageCompression, _getSaveFilePath, category.Id);
```

(b) `src/Honeycomb/App.axaml.cs` — new 出服務並傳入。將：

```csharp
            var excelExport = new ExcelExportService();
```

改為：

```csharp
            var excelExport = new ExcelExportService();
            var imageCompression = new ImageCompressionService();
```

並將：

```csharp
            mainWindow.DataContext = new MainWindowViewModel(db, excelExport, GetSaveFilePath);
```

改為：

```csharp
            mainWindow.DataContext = new MainWindowViewModel(db, excelExport, imageCompression, GetSaveFilePath);
```

(c) `tests/Honeycomb.Tests/ViewModels/ProductListViewModelTests.cs` 的 `CreateVm`（line 38-45）。將：

```csharp
    private ProductListViewModel CreateVm(int categoryId = 1)
    {
        return new ProductListViewModel(
            _db,
            new ExcelExportService(),
            () => Task.FromResult<string?>(null),
            categoryId);
    }
```

改為：

```csharp
    private ProductListViewModel CreateVm(int categoryId = 1)
    {
        return new ProductListViewModel(
            _db,
            new ExcelExportService(),
            new ImageCompressionService(),
            () => Task.FromResult<string?>(null),
            categoryId);
    }
```

- [ ] **Step 5: 跑全部測試確認通過**

Run: `dotnet test`
Expected: 全綠（含既有測試 + `ProductImageDbTests` 2 + `ImageCompressionServiceTests` 4 + `ProductImageTests` 9）。

- [ ] **Step 6: Commit**

```bash
git add src/Honeycomb/ViewModels/ProductListViewModel.cs \
        src/Honeycomb/ViewModels/MainWindowViewModel.cs \
        src/Honeycomb/App.axaml.cs \
        tests/Honeycomb.Tests/ViewModels/ProductListViewModelTests.cs \
        tests/Honeycomb.Tests/ViewModels/ProductImageTests.cs
git commit -m "feat: ProductListViewModel 圖片暫存/載入/upsert/移除邏輯"
```

---

## Task 4: View — 轉換器、新增表單圖片區、預覽面板、檔案選擇器

**Files:**
- Create: `src/Honeycomb/Converters/BytesToBitmapConverter.cs`
- Modify: `src/Honeycomb/Views/ProductListView.axaml`
- Modify: `src/Honeycomb/Views/ProductListView.axaml.cs`

> 本任務為 View/XAML 薄橋接，無自動化測試（對齊既有 spec 慣例：測試架構無 Avalonia headless 設定）。以建置 + Task 5 手動驗證覆蓋。

- [ ] **Step 1: 建立 `Converters/BytesToBitmapConverter.cs`**

```csharp
using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Honeycomb.Converters;

public class BytesToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is byte[] { Length: > 0 } bytes
            ? new Bitmap(new MemoryStream(bytes))
            : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

- [ ] **Step 2: `ProductListView.axaml` — 註冊轉換器、宣告 namespace**

在根 `<UserControl ...>` 的屬性中加入 namespace：

```xml
             xmlns:conv="using:Honeycomb.Converters"
```

在 `<UserControl ...>` 開標籤**之後**、`<Grid ...>` 之前加入資源：

```xml
    <UserControl.Resources>
        <conv:BytesToBitmapConverter x:Key="BytesToBitmap"/>
    </UserControl.Resources>
```

- [ ] **Step 3: `ProductListView.axaml` — 新增表單加圖片區**

在新增表單 WrapPanel 內、「新增商品」按鈕那個 `<StackPanel ...><Button Content="新增商品" .../></StackPanel>` **之前**插入：

```xml
                <StackPanel Margin="0,0,8,8">
                    <TextBlock Text="圖片" Margin="0,0,0,4"/>
                    <StackPanel Orientation="Horizontal" Spacing="4" VerticalAlignment="Center">
                        <Button Content="選擇圖片" Click="OnPickNewImageClicked"/>
                        <Border Width="48" Height="48" Margin="4,0,0,0"
                                BorderBrush="{DynamicResource SystemControlForegroundBaseLowBrush}"
                                BorderThickness="1"
                                IsVisible="{Binding HasNewImage}">
                            <Image Source="{Binding NewImageBytes, Converter={StaticResource BytesToBitmap}}"
                                   Stretch="Uniform"/>
                        </Border>
                        <Button Content="清除" Click="OnClearNewImageClicked"
                                IsVisible="{Binding HasNewImage}"/>
                    </StackPanel>
                </StackPanel>
```

- [ ] **Step 4: `ProductListView.axaml` — 清單列切左右欄 + 預覽面板**

目前 Row 1 內有兩個直接掛 `Grid.Row="1"` 的元素：`<DataGrid Grid.Row="1" Name="ProductGrid" ...>...</DataGrid>` 與其後的浮動搜尋 `<Border Grid.Row="1" ...>...</Border>`。用一個帶兩欄的容器 Grid 包住它們，並加上右側預覽面板。

把這兩個元素整段（從 `<!-- Product DataGrid -->` 到搜尋 `</Border>`）替換為：

```xml
        <!-- Product DataGrid + 圖片預覽 -->
        <Grid Grid.Row="1" ColumnDefinitions="*,Auto">

            <DataGrid Grid.Column="0"
                      Name="ProductGrid"
                      ItemsSource="{Binding Products}"
                      SelectedItem="{Binding SelectedProduct}"
                      SelectionMode="Extended"
                      AutoGenerateColumns="False"
                      IsReadOnly="False"
                      CanUserSortColumns="True"
                      CanUserResizeColumns="True"
                      CellEditEnding="OnCellEditEnding">
                <DataGrid.Resources>
                    <x:Double x:Key="DataGridSortIconMinWidth">12</x:Double>
                </DataGrid.Resources>
                <DataGrid.Styles>
                    <Style Selector="DataGridRow:nth-child(even)">
                        <Setter Property="Background" Value="{DynamicResource SystemListLowColor}"/>
                    </Style>
                </DataGrid.Styles>
                <DataGrid.Columns>
                    <DataGridTextColumn Header="商品名稱" Binding="{Binding Name}" Width="200"/>
                    <DataGridTextColumn Header="單價" Binding="{Binding UnitPrice, StringFormat=N2}" Width="100"/>
                    <DataGridTemplateColumn Header="幣別" Width="100" SortMemberPath="Currency.Code">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate x:DataType="models:Product">
                                <TextBlock Text="{Binding Currency.Code}" VerticalAlignment="Center" Margin="4,0"/>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                        <DataGridTemplateColumn.CellEditingTemplate>
                            <DataTemplate x:CompileBindings="False">
                                <ComboBox ItemsSource="{Binding DataContext.Currencies, RelativeSource={RelativeSource AncestorType=UserControl}}"
                                          SelectedItem="{Binding Currency}">
                                    <ComboBox.ItemTemplate>
                                        <DataTemplate>
                                            <TextBlock Text="{Binding Code}"/>
                                        </DataTemplate>
                                    </ComboBox.ItemTemplate>
                                </ComboBox>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellEditingTemplate>
                    </DataGridTemplateColumn>
                    <DataGridTextColumn Header="匯率" Binding="{Binding ExchangeRate, StringFormat=N2}" Width="100"/>
                    <DataGridTextColumn Header="額外成本" Binding="{Binding ExtraCost, StringFormat=N0}" Width="100"/>
                    <DataGridTextColumn Header="折扣" Binding="{Binding Discount, StringFormat=N2}" Width="80"/>
                    <DataGridTextColumn Header="上架價格" Binding="{Binding ListingPrice, StringFormat=0}" Width="100"/>
                    <DataGridTextColumn Header="手續費(%)" Binding="{Binding CommissionFee, StringFormat=0}" Width="80"/>
                    <DataGridTextColumn Header="成本價" Binding="{Binding CostPrice, StringFormat=N2}" Width="100" IsReadOnly="True"/>
                    <DataGridTextColumn Header="利潤" Binding="{Binding Profit, StringFormat=N2}" Width="100" IsReadOnly="True"/>
                    <DataGridTextColumn Header="利潤率(%)" Binding="{Binding ProfitMargin, StringFormat={}{0:N2}%}" Width="80" IsReadOnly="True"/>
                    <DataGridTextColumn Header="建立時間" Binding="{Binding CreatedAt, StringFormat=yyyy/MM/dd}" Width="120" IsReadOnly="True"/>
                </DataGrid.Columns>
            </DataGrid>

            <!-- Floating Search Overlay（疊在 DataGrid 之上） -->
            <Border Grid.Column="0"
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
                    <Button Content="↑" ToolTip.Tip="上一筆 (Shift+Enter)" Click="OnPreviousMatchClicked"/>
                    <Button Content="↓" ToolTip.Tip="下一筆 (Enter)" Click="OnNextMatchClicked"/>
                    <Button Content="✕" ToolTip.Tip="關閉 (Esc)" Click="OnCloseSearchClicked"/>
                </StackPanel>
            </Border>

            <!-- 圖片預覽面板 -->
            <Border Grid.Column="1" Width="260" Margin="12,0,0,0" Padding="8"
                    BorderBrush="{DynamicResource SystemControlForegroundBaseLowBrush}"
                    BorderThickness="1" CornerRadius="6">
                <StackPanel Spacing="8">
                    <TextBlock Text="商品圖片" FontWeight="Bold"/>
                    <Border Height="200" Background="{DynamicResource SystemListLowColor}">
                        <Panel>
                            <TextBlock Text="未選擇商品或無圖片"
                                       HorizontalAlignment="Center" VerticalAlignment="Center"
                                       TextWrapping="Wrap" TextAlignment="Center"
                                       Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"
                                       IsVisible="{Binding !HasSelectedImage}"/>
                            <Image Stretch="Uniform"
                                   Source="{Binding SelectedImageBytes, Converter={StaticResource BytesToBitmap}}"
                                   IsVisible="{Binding HasSelectedImage}"/>
                        </Panel>
                    </Border>
                    <TextBlock Text="{Binding SelectedProduct.Name}" TextWrapping="Wrap"/>
                    <StackPanel Orientation="Horizontal" Spacing="8">
                        <Button Content="選擇圖片" Click="OnPickImageClicked"/>
                        <Button Content="移除圖片" Click="OnRemoveImageClicked"/>
                    </StackPanel>
                </StackPanel>
            </Border>

        </Grid>
```

- [ ] **Step 5: `ProductListView.axaml.cs` — 加 using 與檔案選擇器事件**

檔案最上方 using 區塊加入：

```csharp
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
```

在類別內（例如 `OnNextMatchClicked` 之後）加入：

```csharp
    private async void OnPickNewImageClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm) return;
        var stream = await PickImageStreamAsync();
        if (stream is null) return;
        await using (stream)
        {
            vm.SetNewImage(stream);
        }
    }

    private void OnClearNewImageClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProductListViewModel vm)
            vm.NewImageBytes = null;
    }

    private async void OnPickImageClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm) return;
        var stream = await PickImageStreamAsync();
        if (stream is null) return;
        await using (stream)
        {
            vm.AttachImageToSelected(stream);
        }
    }

    private void OnRemoveImageClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProductListViewModel vm)
            vm.RemoveImageFromSelected();
    }

    private async Task<Stream?> PickImageStreamAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "選擇商品圖片",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("圖片")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.webp", "*.bmp" }
                }
            }
        });

        if (files.Count == 0) return null;
        return await files[0].OpenReadAsync();
    }
```

- [ ] **Step 6: 建置確認**

Run: `dotnet build Honeycomb.slnx`
Expected: Build succeeded（無錯誤）。

- [ ] **Step 7: Commit**

```bash
git add src/Honeycomb/Converters/BytesToBitmapConverter.cs \
        src/Honeycomb/Views/ProductListView.axaml \
        src/Honeycomb/Views/ProductListView.axaml.cs
git commit -m "feat: 商品圖片新增表單區與預覽面板 UI"
```

---

## Task 5: 整體驗證與手動煙霧測試

**Files:** 無（驗證任務）

- [ ] **Step 1: 全測試 + 建置**

Run: `dotnet test`
Expected: 全綠。
Run: `dotnet build Honeycomb.slnx`
Expected: Build succeeded。

- [ ] **Step 2: 啟動 app 手動驗證**

Run: `dotnet run --project src/Honeycomb`

依序確認：

1. 新增表單填好欄位 → 按「選擇圖片」選一張大圖 → 表單出現 48×48 縮圖、「清除」鈕出現
2. 按「新增商品」→ 清單多一筆；選中該筆 → 右側預覽面板顯示壓縮後的圖、面板顯示商品名稱
3. 選另一筆無圖商品 → 預覽面板顯示「未選擇商品或無圖片」佔位文字
4. 對無圖商品按面板「選擇圖片」→ 圖即時出現
5. 同一筆再「選擇圖片」選別張 → 圖被覆寫（重啟 app 後仍只顯示最後一張，DB 單一 row）
6. 「移除圖片」→ 預覽清空回佔位文字
7. 刪除有圖商品 → 不報錯（其 ProductImage 由 cascade 連帶刪除）
8. 選一個損壞/非圖檔 → 底部出現「無法讀取圖片」、app 不崩潰
9. 重啟 app → 先前掛的圖仍在（確認 migration 已套用、BLOB 已落盤）

- [ ] **Step 3: 確認 migration 已套用到正式 DB**

App 啟動時 `db.Database.Migrate()`（`App.axaml.cs`）會自動套用 `AddProductImage`。若 Step 2 第 9 點通過即代表 `ProductImages` 表已建立並可讀寫。

> 本任務各步驟為驗證；無新檔需 commit。如手動驗證中發現問題，回到對應 Task 修正並重跑該 Task 的測試。

---

## Self-Review

**Spec coverage：**
- 商品可新增圖片（新增時 + 建立後）→ Task 3（`SetNewImage`/`AddProduct`/`AttachImageToSelected`）、Task 4（兩處入口 UI）✓
- 點商品看圖（內嵌預覽面板）→ Task 3（`SelectedProduct`/`SelectedImageBytes`）、Task 4（預覽面板）✓
- 壓縮 → Task 2（SkiaSharp，長邊 1024 / JPEG 80）✓
- 存進 SQLite 獨立 ProductImage 表 → Task 1（model + 1:1 + cascade + migration）✓
- bytes→Bitmap 在 View 層 → Task 4（`BytesToBitmapConverter`）✓
- 測試（壓縮服務 / VM / schema）→ Task 1、Task 2、Task 3 ✓
- cascade 刪除 → Task 1（設定）+ Task 3（`DeleteProduct_CascadeDeletesImage`）✓

**Placeholder scan：** 無 TBD/TODO；migration 檔以萬用字元表示（EF 產生），Step 6 附預期內容供核對。

**Type consistency：** 建構子新參數順序 `(db, excelExport, imageCompression, getSaveFilePath, categoryId)` 在 ProductListViewModel 定義、MainWindowViewModel 呼叫、App 注入、兩個測試的 `CreateVm` 中一致；方法名 `SetNewImage` / `AttachImageToSelected` / `RemoveImageFromSelected`、屬性 `SelectedProduct` / `SelectedImageBytes` / `NewImageBytes` / `HasSelectedImage` / `HasNewImage` 在 VM、測試、XAML binding 中一致。
