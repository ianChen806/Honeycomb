# 商品圖片功能設計規格

**日期：** 2026-05-30
**範圍：** Honeycomb 庫存管理系統 — 商品圖片的新增、儲存（壓縮）、與檢視

## 問題敘述

目前 `Product` 只有文字與數值欄位，無法替商品保留視覺資訊。需求有三：

1. 商品可以新增圖片
2. 點商品時可以看到圖片
3. 儲存的圖片要壓縮，且決定存放位置

## 目標

讓使用者能替每件商品掛上**一張**代表圖，圖片**壓縮後存進 SQLite**，並能在清單旁的預覽面板看到選中商品的圖。新增商品時與建立後都能設定圖片。

## 規格決策摘要

| 項目 | 決策 | 理由 |
|------|------|------|
| 每商品圖片數量 | 單張（1 對 1） | 庫存管理最常見需求，UI/schema 最簡 |
| 儲存位置 | 壓縮後存進 SQLite BLOB | 維持「所有資料在一個 .db 檔」的可攜性 |
| BLOB 放哪張表 | 獨立 `ProductImage` 表（非 Product 欄位） | 避免清單載入把全部圖片位元組拉進記憶體 |
| 新增方式 | 檔案選擇器（Avalonia StorageProvider） | 最標準、內建支援、跨平台 |
| 檢視方式 | 主視窗內嵌預覽面板（選中即顯示） | 不需開新視窗，選哪列看哪列 |
| 設定圖片時機 | 新增表單 **與** 建立後預覽面板皆可 | 兩條路共用壓縮服務與轉換器 |
| 壓縮引擎 | SkiaSharp 2.88.9 | 已是 Avalonia 傳遞依賴，無新增原生二進位負擔 |
| 壓縮參數 | 長邊上限 1024px（只縮不放）、JPEG 品質 80 | 單張約 100–300KB，個人庫存可接受 |
| bytes → 畫面 | VM 暴露 `byte[]?`，View 端轉 `Bitmap` | VM 維持可用純 xUnit + InMemory SQLite 測試 |

## 架構

### 動到的檔案

| 動作 | 檔案 |
|------|------|
| 新增 | `src/Honeycomb/Models/ProductImage.cs` |
| 新增 | `src/Honeycomb/Services/ImageCompressionService.cs` |
| 新增 | `src/Honeycomb/Converters/BytesToBitmapConverter.cs` |
| 新增 | `src/Honeycomb/Migrations/*_AddProductImage.cs`（EF 產生） |
| 修改 | `src/Honeycomb/Data/AppDbContext.cs`（`DbSet<ProductImage>` + 關係設定） |
| 修改 | `src/Honeycomb/ViewModels/ProductListViewModel.cs`（選取/圖片欄位與方法） |
| 修改 | `src/Honeycomb/Views/ProductListView.axaml`（預覽面板 + 新增表單圖片區） |
| 修改 | `src/Honeycomb/Views/ProductListView.axaml.cs`（檔案選擇器事件） |
| 修改 | `src/Honeycomb/Honeycomb.csproj`（顯式 pin `SkiaSharp` 2.88.9） |
| 新增 | `tests/Honeycomb.Tests/Services/ImageCompressionServiceTests.cs` |
| 新增 | `tests/Honeycomb.Tests/ViewModels/ProductImageTests.cs` |

> **`Product.cs` 完全不動。** 1 對 1 關係從 `ProductImage` 那一側配置，Product 上不加 navigation property，確保清單查詢不可能誤帶圖片進記憶體。

### MVVM 分層

```
┌──────────────────────────────────────────────────────────┐
│ ProductListView (View)                                    │
│  ├─ 新增表單「選擇圖片」 ─→ OpenFilePicker ─→ vm.SetNewImage(stream)
│  ├─ 預覽面板「選擇圖片」 ─→ OpenFilePicker ─→ vm.AttachImageToSelected(stream)
│  ├─ 預覽面板「移除圖片」 ─→ vm.RemoveImageFromSelected()
│  ├─ DataGrid SelectedItem ──→ vm.SelectedProduct
│  └─ Image.Source = {Binding NewImageBytes / SelectedImageBytes,
│                      Converter={BytesToBitmapConverter}}
└────────────────────────────┬─────────────────────────────┘
                             │
                             ▼
┌──────────────────────────────────────────────────────────┐
│ ProductListViewModel (VM)                                 │
│  ├─ SelectedProduct : Product?                            │
│  ├─ SelectedImageBytes : byte[]?    ← 選取改變時按需載入   │
│  ├─ NewImageBytes : byte[]?         ← 新增表單暫存（已壓縮）│
│  ├─ SetNewImage(stream)                                   │
│  ├─ AttachImageToSelected(stream)                         │
│  ├─ RemoveImageFromSelected()                             │
│  └─ AddProduct() —— 收尾時一併寫入 ProductImage           │
└────────────────────────────┬─────────────────────────────┘
                             │  ImageCompressionService.Compress(stream) → byte[]
                             ▼
┌──────────────────────────────────────────────────────────┐
│ AppDbContext / SQLite                                     │
│  Products ─┬─ (1:1) ─→ ProductImages                      │
│            └─ FK ProductId (唯一索引, ON DELETE CASCADE)   │
└──────────────────────────────────────────────────────────┘
```

VM 只認 `byte[]`，不碰 `Avalonia.Media.Imaging.Bitmap`：`Bitmap` 建構需要 Avalonia 平台初始化，放進 VM 會讓現有「純 xUnit + InMemory SQLite」測試無法跑。bytes → Bitmap 的轉換留在 View 層的 `IValueConverter`，VM 因此完全可測。

## 資料模型

### `Models/ProductImage.cs`（新增）

```csharp
namespace Honeycomb.Models;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public byte[] Data { get; set; } = [];   // 壓縮後的 JPEG bytes
}
```

刻意最小：不存 Width/Height（Avalonia `Image` 控制項以 `Stretch=Uniform` 自動排版）、不存 ContentType（一律 JPEG）。

### `AppDbContext` 設定（修改）

```csharp
public DbSet<ProductImage> ProductImages => Set<ProductImage>();

modelBuilder.Entity<ProductImage>(entity =>
{
    entity.HasKey(pi => pi.Id);
    entity.HasIndex(pi => pi.ProductId).IsUnique();   // 強制 1 對 1
    entity.Property(pi => pi.Data).IsRequired();
    entity.HasOne<Product>()                          // Product 上不加 nav prop
          .WithOne()
          .HasForeignKey<ProductImage>(pi => pi.ProductId)
          .OnDelete(DeleteBehavior.Cascade);          // 刪商品連帶刪圖
});
```

> SQLite 透過 EF Core 預設啟用 `PRAGMA foreign_keys=ON`，cascade 由 DB 層執行；既有 `DeleteProducts()` 的 `Remove(product)` 無需改動即可連帶刪圖。

## 壓縮服務

### `Services/ImageCompressionService.cs`（新增）

無狀態服務（對齊 `ExcelExportService` 風格），純位元組進出，好測：

```csharp
public class ImageCompressionService
{
    private const int MaxEdge = 1024;
    private const int JpegQuality = 80;

    /// <summary>解碼 → 等比縮放（只縮不放）→ 重新編碼 JPEG。</summary>
    /// <exception cref="InvalidOperationException">來源無法解碼為圖片時。</exception>
    public byte[] Compress(Stream source);
}
```

流程：

```
SKBitmap.Decode(source)
  → 若回 null：throw InvalidOperationException("無法讀取圖片")
  → 計算 scale = min(1, MaxEdge / max(width, height))   // 只縮不放
  → 若 scale < 1：SKBitmap.Resize(新尺寸, 高品質取樣)
  → SKImage.FromBitmap(...).Encode(SKEncodedImageFormat.Jpeg, 80)
  → 回傳 byte[]
```

## bytes → 畫面

### `Converters/BytesToBitmapConverter.cs`（新增）

```csharp
public class BytesToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type t, object? p, CultureInfo c)
        => value is byte[] { Length: > 0 } bytes
            ? new Bitmap(new MemoryStream(bytes))
            : null;

    public object? ConvertBack(...) => throw new NotSupportedException();
}
```

在 `ProductListView` 的 `UserControl.Resources` 註冊一份共用實例，新增表單縮圖與預覽面板大圖共用。

> **生命週期取捨：** 單張預覽圖在「選取改變 / 換圖」時才重建一次，數量與頻率都極低，交給 GC 即可，不額外做 `Bitmap.Dispose()` 管理。若未來改成多圖/縮圖牆再重新評估。

## UI 與互動流程

### 版面調整（`ProductListView.axaml`）

- **Row 0 新增表單**：WrapPanel 末端加一個「圖片」區塊
  ```
  圖片
  [選擇圖片]  [縮圖預覽 ~60x60]  [清除]
  ```
  縮圖 `Image.Source` 綁 `NewImageBytes`（經轉換器）；「清除」鈕在 `NewImageBytes != null` 時才顯示。
- **Row 1 清單列**：切成左右兩欄 `ColumnDefinitions="*,Auto"`
  - 左（`*`）：現有 `DataGrid`
  - 右（`Auto`，固定寬 ~260）：預覽 `Border`，內含
    - 大圖 `Image`（`Stretch=Uniform`，綁 `SelectedImageBytes`），無圖/未選時顯示佔位文字
    - 選中商品名稱
    - `[選擇圖片]` `[移除圖片]` 兩鈕
  - 既有浮動搜尋層維持靠右上，疊在 DataGrid 區域之上（不與預覽面板搶位）
- Row 2 底部列維持滿版不變

### 新增時設定圖片

```
表單「選擇圖片」(code-behind OnPickNewImageClicked)
  → TopLevel.GetTopLevel(this).StorageProvider.OpenFilePickerAsync(圖片過濾 jpg/png/webp/bmp)
  → 取得讀取 stream
  → vm.SetNewImage(stream)
      ├─ bytes = _imageCompression.Compress(stream)
      └─ NewImageBytes = bytes        // 表單縮圖即時顯示
表單「清除」→ NewImageBytes = null
```

`AddProduct()` 收尾調整：

```
_db.Products.Add(product); _db.SaveChanges();      // 取得 product.Id
if (NewImageBytes is not null):
    _db.ProductImages.Add(new ProductImage { ProductId = product.Id, Data = NewImageBytes })
    _db.SaveChanges()
... 連同既有欄位一併把 NewImageBytes 清空，再 LoadData()
```

### 建立後設定/更換/移除圖片

```
選中清單某列 → SelectedProduct setter → OnSelectedProductChanged
  → 按需查詢：_db.ProductImages
                .Where(pi => pi.ProductId == SelectedProduct.Id)
                .Select(pi => pi.Data)
                .FirstOrDefault()
  → SelectedImageBytes = bytes（或 null）

預覽面板「選擇圖片」(code-behind) → OpenFilePicker → vm.AttachImageToSelected(stream)
  ├─ 若 SelectedProduct 為 null → ErrorMessage，return
  ├─ bytes = _imageCompression.Compress(stream)
  ├─ upsert：查 ProductImage by ProductId，有則改 Data，無則 Add
  ├─ _db.SaveChanges()
  └─ SelectedImageBytes = bytes

預覽面板「移除圖片」→ vm.RemoveImageFromSelected()
  ├─ 查 ProductImage by ProductId，存在則 Remove + SaveChanges
  └─ SelectedImageBytes = null
```

### 兩條路的對照

| 時機 | 入口 | VM 暫存/狀態 | 落盤點 |
|------|------|--------------|--------|
| 新增時 | 表單「選擇圖片」 | `NewImageBytes` | `AddProduct()` 內一併寫入 |
| 建立後 | 預覽面板「選擇圖片/移除圖片」 | `SelectedImageBytes` | 該方法內即時寫入 |

兩條路共用 `ImageCompressionService` 與 `BytesToBitmapConverter`，無重複邏輯。

### VM 介面變更（`ProductListViewModel`）

```csharp
// 建構子多注入壓縮服務（對齊既有 _excelExport 注入風格）
public ProductListViewModel(AppDbContext db, ExcelExportService excelExport,
    ImageCompressionService imageCompression, Func<Task<string?>> getSaveFilePath,
    int categoryId = 1);

[ObservableProperty] private Product? _selectedProduct;
[ObservableProperty] private byte[]? _selectedImageBytes;
[ObservableProperty] private byte[]? _newImageBytes;

partial void OnSelectedProductChanged(Product? value);   // 按需載入圖片 bytes
public void SetNewImage(Stream source);                  // 壓縮 → NewImageBytes
public void AttachImageToSelected(Stream source);        // 壓縮 → upsert → SelectedImageBytes
public void RemoveImageFromSelected();                   // 刪 row → 清空
```

> `ProductListViewModel` 僅於 `MainWindowViewModel.cs:52` 建構；`ImageCompressionService` 在 `App.axaml.cs` new 出後，沿 `MainWindowViewModel` 傳入（對齊既有 `ExcelExportService` 的注入鏈：`App.axaml.cs:36` → `MainWindowViewModel` → `ProductListViewModel`）。檔案選擇器走 View code-behind 的 `StorageProvider`（對齊既有刪除/移動的 code-behind 事件模式），不另走 `Func` 注入。

## 錯誤處理 / 邊界

| 邊界 | 處理 |
|------|------|
| 選的檔案不是有效圖片 / 解碼失敗 | `Compress` 丟例外 → 上層 catch → `ErrorMessage = "無法讀取圖片"` |
| 未選商品就按預覽面板「選擇圖片」 | `ErrorMessage` 提示，不動作 |
| 檔案選擇器被取消（回 null） | 直接 return，無副作用 |
| 對已有圖的商品再選圖 | upsert：覆寫既有 `ProductImage.Data` |
| 對無圖商品按「移除圖片」 | 查無 row → 無動作 |
| 刪除商品 | DB cascade 連帶刪 `ProductImage` |
| 超大來源檔 | 以 `SKBitmap.Decode` 串流解碼；壓縮後僅存縮放結果，原檔不落盤 |

錯誤一律走既有 `ErrorMessage` 屬性顯示，不靜默吞。

## 測試策略

### 壓縮服務（`Services/ImageCompressionServiceTests.cs`，新增）

用 SkiaSharp 即時產生測試圖（不需 fixture 檔；SkiaSharp 在測試 RID 的原生資產已還原，無需 Avalonia headless）：

1. **`Compress_DownscalesLargeImage_ToMaxEdge`**：產生 4000×3000 → 壓完長邊 ≤ 1024，且仍可被 `SKBitmap.Decode` 重新解碼
2. **`Compress_DoesNotUpscaleSmallImage`**：產生 500×400 → 壓完尺寸 ≤ 原尺寸（不放大）
3. **`Compress_OutputIsJpeg`**：壓完 bytes 的檔頭符合 JPEG（`FF D8 FF`）
4. **`Compress_InvalidBytes_Throws`**：丟非圖片 bytes → `InvalidOperationException`

### ViewModel（`ViewModels/ProductImageTests.cs`，新增）

延用既有 xUnit + InMemory SQLite 架構，圖片 bytes 用壓縮服務即時產生：

1. **`SetNewImage_CompressesAndStages`**：`SetNewImage(stream)` 後 `NewImageBytes` 非空且小於原始
2. **`AddProduct_WithStagedImage_CreatesProductAndImageRow`**：先 `SetNewImage` 再 `AddProduct` → `Products` 多一筆、`ProductImages` 有對應 row 且 `Data` == 暫存 bytes、`NewImageBytes` 歸 null
3. **`AddProduct_WithoutImage_CreatesNoImageRow`**：未設圖 → `ProductImages` 為空
4. **`SelectedProduct_LoadsImageBytes`**：選中有圖商品 → `SelectedImageBytes` 載入；選中無圖商品 → null
5. **`AttachImageToSelected_UpsertsRow`**：對無圖商品掛圖 → 新增 row；再掛一次 → 覆寫同一 row（仍只有一筆）
6. **`AttachImageToSelected_NoSelection_SetsError`**：未選商品 → `ErrorMessage` 非空、不新增 row
7. **`RemoveImageFromSelected_DeletesRow`**：移除 → row 消失、`SelectedImageBytes` 為 null
8. **`DeleteProduct_CascadeDeletesImage`**：刪有圖商品 → `ProductImages` 對應 row 一併消失

### 手動驗證腳本

1. 新增表單填好欄位 → 按「選擇圖片」選一張大圖 → 確認表單出現縮圖 → 「新增商品」→ 清單多一筆
2. 選中該筆 → 右側預覽面板顯示壓縮後的圖
3. 對另一筆無圖商品 → 預覽面板「選擇圖片」→ 圖即時出現
4. 同一筆再「選擇圖片」選別張 → 圖被覆寫（DB 仍只有一筆 ProductImage）
5. 「移除圖片」→ 預覽清空，DB row 消失
6. 刪除有圖商品 → 確認 `ProductImages` 連帶清掉
7. 選一個損壞/非圖檔 → 出現「無法讀取圖片」錯誤、不崩潰

### 不寫 UI 整合測試的理由

對齊既有 spec 慣例：現有 test 架構無 Avalonia headless 設定，檔案選擇器與 `BytesToBitmapConverter` 屬 View 層薄橋接，靠手動驗證 + VM/服務單元測試已足夠覆蓋風險。

## Out of Scope（YAGNI）

- 多張圖 / 相簿
- 拖放、剪貼簿貼上（本次僅檔案選擇器）
- 獨立縮圖表（單張、僅選取時載入，不需預生縮圖）
- Excel 匯出帶圖
- 圖片裁切 / 旋轉 / 編輯
- WebP 等其他輸出格式（先固定 JPEG）
