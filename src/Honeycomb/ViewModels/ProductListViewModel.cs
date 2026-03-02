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
    private decimal? _newQuantity = 1;

    [ObservableProperty]
    private decimal? _newUnitPrice;

    [ObservableProperty]
    private Currency? _newCurrency;

    [ObservableProperty]
    private decimal? _newExchangeRate;

    [ObservableProperty]
    private decimal? _newDiscount = 1m;

    [ObservableProperty]
    private decimal? _newListingPrice;

    [ObservableProperty]
    private decimal? _newCommissionFee = 15m;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _costPricePreview = string.Empty;

    [ObservableProperty]
    private string _profitPreview = string.Empty;

    [ObservableProperty]
    private string _profitMarginPreview = string.Empty;

    public ProductListViewModel(AppDbContext db, ExcelExportService excelExport, Func<Task<string?>> getSaveFilePath)
    {
        _db = db;
        _excelExport = excelExport;
        _getSaveFilePath = getSaveFilePath;
        LoadData();
    }

    public void LoadData()
    {
        _db.ChangeTracker.Clear();
        Products.Clear();

        var selectedId = NewCurrency?.Id;

        Currencies.Clear();
        foreach (var currency in _db.Currencies.ToList())
        {
            Currencies.Add(currency);
        }

        if (selectedId is { } id)
        {
            NewCurrency = Currencies.FirstOrDefault(c => c.Id == id);
        }

        foreach (var product in _db.Products.Include(p => p.Currency).OrderBy(p => p.Name).ToList())
        {
            Products.Add(product);
        }
    }

    partial void OnNewUnitPriceChanged(decimal? value) => UpdatePricePreview();
    partial void OnNewDiscountChanged(decimal? value) => UpdatePricePreview();
    partial void OnNewExchangeRateChanged(decimal? value) => UpdatePricePreview();
    partial void OnNewListingPriceChanged(decimal? value) => UpdatePricePreview();
    partial void OnNewCommissionFeeChanged(decimal? value) => UpdatePricePreview();

    private void UpdatePricePreview()
    {
        var unitPrice = NewUnitPrice ?? 0;
        var exchangeRate = NewExchangeRate ?? 0;
        var discount = NewDiscount ?? 1;
        var listingPrice = NewListingPrice ?? 0;
        var commissionFee = NewCommissionFee ?? 15;

        var costPrice = unitPrice * exchangeRate * discount + listingPrice * (commissionFee / 100m);
        var profit = listingPrice - costPrice;
        var profitMargin = listingPrice > 0 ? (profit / listingPrice) * 100m : 0;

        CostPricePreview = $"成本價: {costPrice:N2}";
        ProfitPreview = $"利潤: {profit:N2}";
        ProfitMarginPreview = $"利潤率: {profitMargin:N2}%";
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

        if (NewListingPrice is not { } listingPrice || listingPrice < 0)
        {
            ErrorMessage = "上架價格不能為負數";
            return;
        }

        if (NewCommissionFee is not { } commissionFee || commissionFee < 0)
        {
            ErrorMessage = "手續費不能為負數";
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
            ListingPrice = listingPrice,
            CommissionFee = commissionFee,
            CreatedAt = DateTime.Now
        };

        _db.Products.Add(product);
        _db.SaveChanges();

        NewName = string.Empty;
        NewQuantity = 1;
        NewUnitPrice = null;
        NewListingPrice = null;

        LoadData();
    }

    public void SaveProductChanges()
    {
        _db.SaveChanges();
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
