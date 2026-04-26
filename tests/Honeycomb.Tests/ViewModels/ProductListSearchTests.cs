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
}
