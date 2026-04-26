using System;
using System.Collections.Generic;
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
    private readonly int _categoryId;

    public int CategoryId => _categoryId;
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<Currency> Currencies { get; } = [];

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private decimal? _newExtraCost = 0;

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

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isSearchVisible;

    [ObservableProperty]
    private string _matchCountText = "0/0";

    private List<Product> _matches = [];
    private int _currentMatchIndex = -1;

    public ProductListViewModel(AppDbContext db, ExcelExportService excelExport, Func<Task<string?>> getSaveFilePath, int categoryId = 1)
    {
        _db = db;
        _excelExport = excelExport;
        _getSaveFilePath = getSaveFilePath;
        _categoryId = categoryId;
        LoadData();
    }

    public void LoadData()
    {
        SearchQuery = string.Empty;
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

        var query = _db.Products.Include(p => p.Currency)
            .Where(p => p.CategoryId == _categoryId);

        foreach (var product in query.OrderBy(p => p.Name).ToList())
        {
            Products.Add(product);
        }
    }

    partial void OnNewExtraCostChanged(decimal? value) => UpdatePricePreview();
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

        var extraCost = NewExtraCost ?? 0;

        var costPrice = unitPrice * exchangeRate * discount + listingPrice * (commissionFee / 100m) + extraCost;
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

        if (NewExtraCost is not { } extraCost || extraCost < 0)
        {
            ErrorMessage = "額外成本不能為負數";
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
            ExtraCost = extraCost,
            UnitPrice = unitPrice,
            CurrencyId = NewCurrency.Id,
            ExchangeRate = exchangeRate,
            Discount = discount,
            ListingPrice = listingPrice,
            CommissionFee = commissionFee,
            CategoryId = _categoryId,

            CreatedAt = DateTime.Now
        };

        _db.Products.Add(product);
        _db.SaveChanges();

        NewName = string.Empty;
        NewExtraCost = 0;
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

    public System.Collections.Generic.List<Category> GetOtherCategories()
    {
        return _db.Categories
            .Where(c => c.Id != _categoryId)
            .OrderBy(c => c.Id)
            .ToList();
    }

    public event Action? ProductsMoved;

    public event Action<Product>? MatchScrollRequested;

    public void MoveProducts(System.Collections.Generic.IReadOnlyList<Product> products, int targetCategoryId)
    {
        ErrorMessage = string.Empty;

        foreach (var product in products)
        {
            var entity = _db.Products.Find(product.Id);
            if (entity is not null)
            {
                entity.CategoryId = targetCategoryId;
            }
        }

        _db.SaveChanges();
        LoadData();
        ProductsMoved?.Invoke();
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

    partial void OnSearchQueryChanged(string value)
    {
        RecomputeMatches();
    }

    private void RecomputeMatches()
    {
        if (string.IsNullOrEmpty(SearchQuery))
        {
            _matches = [];
            _currentMatchIndex = -1;
            MatchCountText = "0/0";
            return;
        }

        _matches = Products
            .Where(p => p.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _currentMatchIndex = _matches.Count > 0 ? 0 : -1;
        UpdateMatchCountText();

        if (_currentMatchIndex >= 0)
        {
            MatchScrollRequested?.Invoke(_matches[_currentMatchIndex]);
        }
    }

    public void NextMatch()
    {
        if (_matches.Count == 0) return;
        _currentMatchIndex = (_currentMatchIndex + 1) % _matches.Count;
        UpdateMatchCountText();
        MatchScrollRequested?.Invoke(_matches[_currentMatchIndex]);
    }

    public void PreviousMatch()
    {
        if (_matches.Count == 0) return;
        _currentMatchIndex = (_currentMatchIndex - 1 + _matches.Count) % _matches.Count;
        UpdateMatchCountText();
        MatchScrollRequested?.Invoke(_matches[_currentMatchIndex]);
    }

    private void UpdateMatchCountText()
    {
        if (_matches.Count == 0)
        {
            MatchCountText = "0/0";
        }
        else
        {
            MatchCountText = $"{_currentMatchIndex + 1}/{_matches.Count}";
        }
    }
}
