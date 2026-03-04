using Honeycomb.Data;
using Honeycomb.Models;
using Honeycomb.Services;
using Honeycomb.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb.Tests.ViewModels;

public class ProductListViewModelTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public ProductListViewModelTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        // Seed a currency for product creation
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

    private int AddCategory(string name)
    {
        var category = new Category { Name = name };
        _db.Categories.Add(category);
        _db.SaveChanges();
        return category.Id;
    }

    [Fact]
    public void MoveProducts_UpdatesCategoryId()
    {
        var targetCategoryId = AddCategory("電子產品");
        var product = AddProduct("Widget A");
        var vm = CreateVm(1);

        vm.MoveProducts([product], targetCategoryId);

        var moved = _db.Products.Find(product.Id);
        Assert.NotNull(moved);
        Assert.Equal(targetCategoryId, moved!.CategoryId);
    }

    [Fact]
    public void MoveProducts_RemovesFromCurrentList()
    {
        var targetCategoryId = AddCategory("電子產品");
        AddProduct("Widget A");
        AddProduct("Widget B");
        var vm = CreateVm(1);
        Assert.Equal(2, vm.Products.Count);

        var toMove = vm.Products.Where(p => p.Name == "Widget A").ToList();
        vm.MoveProducts(toMove, targetCategoryId);

        Assert.Single(vm.Products);
        Assert.Equal("Widget B", vm.Products[0].Name);
    }

    [Fact]
    public void MoveProducts_BatchMove_AllUpdated()
    {
        var targetCategoryId = AddCategory("電子產品");
        var p1 = AddProduct("Widget A");
        var p2 = AddProduct("Widget B");
        var vm = CreateVm(1);

        vm.MoveProducts([p1, p2], targetCategoryId);

        Assert.Empty(vm.Products);
        Assert.Equal(targetCategoryId, _db.Products.Find(p1.Id)!.CategoryId);
        Assert.Equal(targetCategoryId, _db.Products.Find(p2.Id)!.CategoryId);
    }

    [Fact]
    public void GetOtherCategories_ExcludesCurrentCategory()
    {
        AddCategory("電子產品");
        AddCategory("食品");
        var vm = CreateVm(1);

        var others = vm.GetOtherCategories();

        Assert.DoesNotContain(others, c => c.Id == 1);
        Assert.Equal(2, others.Count);
    }
}
