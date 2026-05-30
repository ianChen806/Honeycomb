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
