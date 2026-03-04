using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Honeycomb.Models;

public partial class Product : ObservableObject
{
    public int Id { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private decimal _extraCost;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private int _currencyId;

    [ObservableProperty]
    private Currency? _currency;

    [ObservableProperty]
    private decimal _exchangeRate;

    [ObservableProperty]
    private decimal _discount = 1m;

    [ObservableProperty]
    private decimal _listingPrice;

    [ObservableProperty]
    private decimal _commissionFee = 15m;

    public decimal CostPrice => UnitPrice * ExchangeRate * Discount + ListingPrice * (CommissionFee / 100m) + ExtraCost;
    public decimal Profit => ListingPrice - CostPrice;
    public decimal ProfitMargin => ListingPrice > 0 ? (Profit / ListingPrice) * 100m : 0m;

    [ObservableProperty]
    private int _categoryId = 1;

    [ObservableProperty]
    private Category? _category;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    partial void OnExtraCostChanged(decimal value) => NotifyComputedProperties();
    partial void OnUnitPriceChanged(decimal value) => NotifyComputedProperties();
    partial void OnExchangeRateChanged(decimal value) => NotifyComputedProperties();
    partial void OnDiscountChanged(decimal value) => NotifyComputedProperties();
    partial void OnListingPriceChanged(decimal value) => NotifyComputedProperties();
    partial void OnCommissionFeeChanged(decimal value) => NotifyComputedProperties();

    partial void OnCurrencyChanged(Currency? value)
    {
        if (value is not null)
            CurrencyId = value.Id;
    }

    private void NotifyComputedProperties()
    {
        OnPropertyChanged(nameof(CostPrice));
        OnPropertyChanged(nameof(Profit));
        OnPropertyChanged(nameof(ProfitMargin));
    }
}
