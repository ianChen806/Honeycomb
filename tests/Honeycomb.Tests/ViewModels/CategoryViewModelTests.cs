using Honeycomb.Data;
using Honeycomb.Models;
using Honeycomb.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Honeycomb.Tests.ViewModels;

public class CategoryViewModelTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public CategoryViewModelTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public void AddCategory_ValidName_AddsToCategories()
    {
        var vm = new CategoryViewModel(_db);
        var initialCount = vm.Categories.Count; // "預設" is seeded
        vm.NewCategoryName = "日用品";

        vm.AddCategoryCommand.Execute(null);

        Assert.Equal(initialCount + 1, vm.Categories.Count);
        Assert.Contains(vm.Categories, c => c.Name == "日用品");
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void AddCategory_DuplicateName_ShowsError()
    {
        _db.Categories.Add(new Category { Name = "日用品" });
        _db.SaveChanges();

        var vm = new CategoryViewModel(_db);
        var countBefore = vm.Categories.Count;
        vm.NewCategoryName = "日用品";
        vm.AddCategoryCommand.Execute(null);

        Assert.Equal(countBefore, vm.Categories.Count);
        Assert.Contains("已存在", vm.ErrorMessage);
    }

    [Fact]
    public void AddCategory_EmptyName_ShowsError()
    {
        var vm = new CategoryViewModel(_db);
        var initialCount = vm.Categories.Count;
        vm.NewCategoryName = "";

        vm.AddCategoryCommand.Execute(null);

        Assert.Equal(initialCount, vm.Categories.Count);
        Assert.Contains("必填", vm.ErrorMessage);
    }

    [Fact]
    public void RenameCategory_ValidName_UpdatesName()
    {
        _db.Categories.Add(new Category { Name = "日用品" });
        _db.SaveChanges();

        var vm = new CategoryViewModel(_db);
        var result = vm.RenameCategory(vm.Categories[0].Id, "生活用品");

        Assert.True(result);
        Assert.Equal("生活用品", vm.Categories[0].Name);
    }

    [Fact]
    public void RenameCategory_DuplicateName_ReturnsFalse()
    {
        _db.Categories.Add(new Category { Name = "日用品" });
        _db.Categories.Add(new Category { Name = "生活用品" });
        _db.SaveChanges();

        var vm = new CategoryViewModel(_db);
        var result = vm.RenameCategory(vm.Categories[0].Id, "生活用品");

        Assert.False(result);
        Assert.Contains("已存在", vm.ErrorMessage);
    }

    [Fact]
    public void DeleteCategory_Empty_RemovesCategory()
    {
        _db.Categories.Add(new Category { Name = "日用品" });
        _db.SaveChanges();

        var vm = new CategoryViewModel(_db);
        var target = vm.Categories.First(c => c.Name == "日用品");
        var hasProducts = vm.CategoryHasProducts(target.Id);
        var countBefore = vm.Categories.Count;
        vm.DeleteCategory(target.Id);

        Assert.False(hasProducts);
        Assert.Equal(countBefore - 1, vm.Categories.Count);
        Assert.DoesNotContain(vm.Categories, c => c.Name == "日用品");
    }

    [Fact]
    public void DeleteCategory_WithProducts_MovesProductsToDefaultCategory()
    {
        var category = new Category { Name = "日用品" };
        _db.Categories.Add(category);
        _db.SaveChanges();

        var currency = new Currency { Code = "USD", Name = "美元" };
        _db.Currencies.Add(currency);
        _db.SaveChanges();

        _db.Products.Add(new Product
        {
            Name = "Test Product",
            UnitPrice = 100m,
            CurrencyId = currency.Id,
            ExchangeRate = 31.5m,
            CategoryId = category.Id
        });
        _db.SaveChanges();

        var vm = new CategoryViewModel(_db);
        Assert.True(vm.CategoryHasProducts(category.Id));

        vm.DeleteCategory(category.Id);

        Assert.DoesNotContain(vm.Categories, c => c.Name == "日用品");
        var product = _db.Products.First();
        Assert.Equal(MainWindowViewModel.DefaultCategoryId, product.CategoryId);
    }

    [Fact]
    public void AddCategory_FiresCategoriesChangedEvent()
    {
        var vm = new CategoryViewModel(_db);
        var fired = false;
        vm.CategoriesChanged += () => fired = true;

        vm.NewCategoryName = "日用品";
        vm.AddCategoryCommand.Execute(null);

        Assert.True(fired);
    }

    [Fact]
    public void DeleteCategory_FiresCategoriesChangedEvent()
    {
        _db.Categories.Add(new Category { Name = "日用品" });
        _db.SaveChanges();

        var vm = new CategoryViewModel(_db);
        var fired = false;
        vm.CategoriesChanged += () => fired = true;

        var target = vm.Categories.First(c => c.Name == "日用品");
        vm.DeleteCategory(target.Id);

        Assert.True(fired);
    }
}
