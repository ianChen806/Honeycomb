using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Honeycomb.Data;
using Honeycomb.Models;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb.ViewModels;

public partial class CurrencySettingsViewModel : ViewModelBase
{
    private readonly AppDbContext _db;

    public event Action? CurrenciesChanged;

    public ObservableCollection<Currency> Currencies { get; } = [];

    [ObservableProperty]
    private string _newCode = string.Empty;

    [ObservableProperty]
    private string _newName = string.Empty;

    [ObservableProperty]
    private Currency? _selectedCurrency;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public CurrencySettingsViewModel(AppDbContext db)
    {
        _db = db;
        LoadCurrencies();
    }

    private void LoadCurrencies()
    {
        Currencies.Clear();
        foreach (var currency in _db.Currencies.AsNoTracking().ToList())
        {
            Currencies.Add(currency);
        }
    }

    [RelayCommand]
    private void AddCurrency()
    {
        ErrorMessage = string.Empty;

        var code = NewCode.Trim();
        var name = NewName.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "幣別代碼和名稱都是必填的";
            return;
        }

        if (_db.Currencies.Any(c => c.Code == code))
        {
            ErrorMessage = $"幣別代碼 '{code}' 已存在";
            return;
        }

        var currency = new Currency { Code = code, Name = name };
        _db.Currencies.Add(currency);
        _db.SaveChanges();

        NewCode = string.Empty;
        NewName = string.Empty;

        LoadCurrencies();
        CurrenciesChanged?.Invoke();
    }

    [RelayCommand]
    private void DeleteCurrency()
    {
        ErrorMessage = string.Empty;

        if (SelectedCurrency is null)
            return;

        var inUse = _db.Products.Any(p => p.CurrencyId == SelectedCurrency.Id);
        if (inUse)
        {
            ErrorMessage = $"幣別 '{SelectedCurrency.Code}' 正在被商品使用中，無法刪除";
            return;
        }

        var entity = _db.Currencies.Find(SelectedCurrency.Id);
        if (entity is not null)
        {
            _db.Currencies.Remove(entity);
            _db.SaveChanges();
        }

        SelectedCurrency = null;
        LoadCurrencies();
        CurrenciesChanged?.Invoke();
    }
}
