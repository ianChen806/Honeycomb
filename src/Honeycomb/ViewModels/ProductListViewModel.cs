using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Honeycomb.Data;
using Honeycomb.Models;
using Honeycomb.Services;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb.ViewModels;

public partial class ProductListViewModel : ViewModelBase
{
    private readonly AppDbContext _db;
    private readonly ExcelExportService _excelExport;
    private readonly Func<Task<string?>> _getSaveFilePath;

    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<Currency> Currencies { get; } = [];

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private decimal? _newQuantity;

    [ObservableProperty]
    private decimal? _newUnitPrice;

    [ObservableProperty]
    private Currency? _newCurrency;

    [ObservableProperty]
    private decimal? _newExchangeRate;

    [ObservableProperty]
    private decimal? _newDiscount = 1.0m;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _costPricePreview = string.Empty;

    [ObservableProperty]
    private string _totalPricePreview = string.Empty;

    public ProductListViewModel(AppDbContext db, ExcelExportService excelExport, Func<Task<string?>> getSaveFilePath)
    {
        _db = db;
        _excelExport = excelExport;
        _getSaveFilePath = getSaveFilePath;
        LoadData();
    }

    public void LoadData()
    {
        Products.Clear();
        foreach (var product in _db.Products.Include(p => p.Currency).AsNoTracking().ToList())
        {
            Products.Add(product);
        }

        var selectedId = NewCurrency?.Id;

        Currencies.Clear();
        foreach (var currency in _db.Currencies.AsNoTracking().ToList())
        {
            Currencies.Add(currency);
        }

        if (selectedId is { } id)
        {
            NewCurrency = Currencies.FirstOrDefault(c => c.Id == id);
        }
    }

    partial void OnNewUnitPriceChanged(decimal? value) => UpdatePricePreview();
    partial void OnNewDiscountChanged(decimal? value) => UpdatePricePreview();
    partial void OnNewQuantityChanged(decimal? value) => UpdatePricePreview();
    partial void OnNewExchangeRateChanged(decimal? value) => UpdatePricePreview();

    private void UpdatePricePreview()
    {
        if (NewUnitPrice is { } unitPrice && NewDiscount is { } discount)
        {
            var costPrice = unitPrice * discount;
            CostPricePreview = $"成本價: {costPrice:N2}";

            if (NewQuantity is { } qty && NewExchangeRate is { } rate)
            {
                var totalPrice = qty * unitPrice * rate * discount;
                TotalPricePreview = $"總價: {totalPrice:N2}";
            }
            else
            {
                TotalPricePreview = string.Empty;
            }
        }
        else
        {
            CostPricePreview = string.Empty;
            TotalPricePreview = string.Empty;
        }
    }

    [RelayCommand]
    private void AddProduct()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewName))
        {
            ErrorMessage = "商品名稱為必填";
            return;
        }

        if (NewQuantity is not { } quantity || quantity <= 0)
        {
            ErrorMessage = "數量必須為正整數";
            return;
        }

        if (NewUnitPrice is not { } unitPrice || unitPrice <= 0)
        {
            ErrorMessage = "單價必須為正數";
            return;
        }

        if (NewCurrency is null)
        {
            ErrorMessage = "請選擇幣別";
            return;
        }

        if (NewExchangeRate is not { } exchangeRate || exchangeRate <= 0)
        {
            ErrorMessage = "匯率必須為正數";
            return;
        }

        if (NewDiscount is not { } discount || discount <= 0 || discount > 1)
        {
            ErrorMessage = "折扣必須在 0 到 1 之間（如 0.9 = 九折）";
            return;
        }

        var product = new Product
        {
            Name = NewName.Trim(),
            Quantity = (int)quantity,
            UnitPrice = unitPrice,
            CurrencyId = NewCurrency.Id,
            ExchangeRate = exchangeRate,
            Discount = discount,
            CreatedAt = DateTime.Now
        };

        _db.Products.Add(product);
        _db.SaveChanges();

        NewName = string.Empty;
        NewQuantity = null;
        NewUnitPrice = null;

        LoadData();
    }

    public void DeleteProducts(System.Collections.Generic.IReadOnlyList<Product> products)
    {
        ErrorMessage = string.Empty;

        foreach (var product in products)
        {
            var entity = _db.Products.Find(product.Id);
            if (entity is not null)
            {
                _db.Products.Remove(entity);
            }
        }

        _db.SaveChanges();
        LoadData();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        ErrorMessage = string.Empty;

        if (Products.Count == 0)
        {
            ErrorMessage = "沒有商品可以匯出";
            return;
        }

        var filePath = await _getSaveFilePath();
        if (filePath is null)
            return;

        var products = _db.Products.Include(p => p.Currency).AsNoTracking().ToList();
        _excelExport.Export(products, filePath);
    }
}
