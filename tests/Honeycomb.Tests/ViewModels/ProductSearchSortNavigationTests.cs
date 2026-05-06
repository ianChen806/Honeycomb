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

        var ordered = vm.Products.OrderBy(p => p.Name).ToList();
        vm.OrderedProductsProvider = () => ordered;

        Product? firstMatch = null;
        vm.MatchScrollRequested += p => firstMatch ??= p;

        vm.SearchQuery = "Widget";

        Assert.Equal("1/3", vm.MatchCountText);
        Assert.NotNull(firstMatch);
        Assert.Equal("Widget A", firstMatch!.Name);
    }

    [Fact]
    public void NextMatch_FollowsProviderOrder()
    {
        AddProduct("Widget B");
        AddProduct("Widget A");
        AddProduct("Widget C");
        var vm = CreateVm(1);

        var ordered = vm.Products.OrderBy(p => p.Name).ToList();
        vm.OrderedProductsProvider = () => ordered;

        vm.SearchQuery = "Widget";

        Product? scrolled = null;
        vm.MatchScrollRequested += p => scrolled = p;

        vm.NextMatch();
        Assert.Equal("2/3", vm.MatchCountText);
        Assert.Equal("Widget B", scrolled!.Name);

        vm.NextMatch();
        Assert.Equal("3/3", vm.MatchCountText);
        Assert.Equal("Widget C", scrolled!.Name);
    }

    [Fact]
    public void RecomputeMatches_FallsBackToProducts_WhenProviderNull()
    {
        AddProduct("Widget A");
        AddProduct("Widget B");
        var vm = CreateVm(1);

        vm.SearchQuery = "Widget";

        Assert.Equal("1/2", vm.MatchCountText);
    }
}
