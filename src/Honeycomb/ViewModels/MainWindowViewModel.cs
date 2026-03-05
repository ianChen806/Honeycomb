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

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly AppDbContext _db;
    private readonly ExcelExportService _excelExport;
    private readonly Func<Task<string?>> _getSaveFilePath;

    public const int DefaultCategoryId = 1;

    public CurrencySettingsViewModel CurrencySettings { get; }
    public CategoryViewModel CategoryManager { get; }
    public ObservableCollection<CategoryTabItem> CategoryTabs { get; } = [];

    [ObservableProperty]
    private object? _selectedTab;

    public MainWindowViewModel(AppDbContext db, ExcelExportService excelExport, Func<Task<string?>> getSaveFilePath)
    {
        _db = db;
        _excelExport = excelExport;
        _getSaveFilePath = getSaveFilePath;

        CurrencySettings = new CurrencySettingsViewModel(db);
        CategoryManager = new CategoryViewModel(db);

        CurrencySettings.CurrenciesChanged += ReloadAllProductLists;
        CategoryManager.CategoriesChanged += RebuildCategoryTabs;

        RebuildCategoryTabs();
    }

    private void RebuildCategoryTabs()
    {
        CategoryTabs.Clear();

        // Default category (Id=1) is included naturally via the database seed
        foreach (var category in _db.Categories.OrderBy(c => c.SortOrder).ToList())
        {
            var productList = new ProductListViewModel(_db, _excelExport, _getSaveFilePath, category.Id);
            productList.ProductsMoved += ReloadAllProductLists;
            CategoryTabs.Add(new CategoryTabItem(category.Id, category.Name, productList));
        }
    }

    [ObservableProperty]
    private string _exportErrorMessage = string.Empty;

    [RelayCommand]
    private async Task ExportAllExcel()
    {
        ExportErrorMessage = string.Empty;

        var sheets = new List<(string SheetName, IReadOnlyList<Product> Products)>();

        foreach (var tab in CategoryTabs)
        {
            var products = _db.Products.Include(p => p.Currency)
                .Where(p => p.CategoryId == tab.CategoryId).AsNoTracking().ToList();
            sheets.Add((tab.Header, products));
        }

        if (sheets.All(s => s.Products.Count == 0))
        {
            ExportErrorMessage = "沒有商品可以匯出";
            return;
        }

        var filePath = await _getSaveFilePath();
        if (filePath is null) return;

        _excelExport.Export(sheets, filePath);
    }

    private void ReloadAllProductLists()
    {
        foreach (var tab in CategoryTabs)
        {
            tab.ProductList.LoadData();
        }
    }
}
