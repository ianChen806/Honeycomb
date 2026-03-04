using System;
using System.Linq;
using Honeycomb.Data;
using Honeycomb.Models;
using Honeycomb.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb.Tests.ViewModels;

public class CurrencySettingsViewModelTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public CurrencySettingsViewModelTests()
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
    public void AddCurrency_ValidInput_AddsToCurrencies()
    {
        var vm = new CurrencySettingsViewModel(_db);
        vm.NewCode = "USD";
        vm.NewName = "美元";

        vm.AddCurrencyCommand.Execute(null);

        Assert.Single(vm.Currencies);
        Assert.Equal("USD", vm.Currencies[0].Code);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void AddCurrency_DuplicateCode_ShowsError()
    {
        _db.Currencies.Add(new Currency { Code = "USD", Name = "美元" });
        _db.SaveChanges();

        var vm = new CurrencySettingsViewModel(_db);
        vm.NewCode = "USD";
        vm.NewName = "美金";
        vm.AddCurrencyCommand.Execute(null);

        Assert.Contains("已存在", vm.ErrorMessage);
    }

    [Fact]
    public void AddCurrency_EmptyFields_ShowsError()
    {
        var vm = new CurrencySettingsViewModel(_db);
        vm.NewCode = "";
        vm.NewName = "";

        vm.AddCurrencyCommand.Execute(null);

        Assert.Contains("必填", vm.ErrorMessage);
        Assert.Empty(vm.Currencies);
    }

    [Fact]
    public void DeleteCurrency_NotInUse_RemovesCurrency()
    {
        _db.Currencies.Add(new Currency { Code = "EUR", Name = "歐元" });
        _db.SaveChanges();

        var vm = new CurrencySettingsViewModel(_db);
        vm.SelectedCurrency = vm.Currencies[0];
        vm.DeleteCurrencyCommand.Execute(null);

        Assert.Empty(vm.Currencies);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public void DeleteCurrency_InUse_ShowsErrorAndKeepsCurrency()
    {
        var currency = new Currency { Code = "JPY", Name = "日圓" };
        _db.Currencies.Add(currency);
        _db.SaveChanges();

        _db.Products.Add(new Product
        {
            Name = "Test",
            ExtraCost = 0,
            UnitPrice = 100m,
            CurrencyId = currency.Id,
            ExchangeRate = 0.22m,
            CategoryId = 1
        });
        _db.SaveChanges();

        var vm = new CurrencySettingsViewModel(_db);
        vm.SelectedCurrency = vm.Currencies[0];
        vm.DeleteCurrencyCommand.Execute(null);

        Assert.Single(vm.Currencies);
        Assert.Contains("無法刪除", vm.ErrorMessage);
    }
}
