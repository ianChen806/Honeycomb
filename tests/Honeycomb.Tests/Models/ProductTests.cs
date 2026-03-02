using Honeycomb.Models;

namespace Honeycomb.Tests.Models;

public class ProductTests
{
    [Theory]
    [InlineData(100, 1, 0.9, 0, 15, 90)]
    [InlineData(100, 31.5, 1.0, 500, 15, 3225)]
    [InlineData(200, 0.22, 0.85, 1000, 10, 137.4)]
    public void CostPrice_Calculation(
        decimal unitPrice, decimal exchangeRate, decimal discount,
        decimal listingPrice, decimal commissionFee, decimal expected)
    {
        var product = new Product
        {
            Name = "Test",
            UnitPrice = unitPrice,
            CurrencyId = 1,
            ExchangeRate = exchangeRate,
            Discount = discount,
            ListingPrice = listingPrice,
            CommissionFee = commissionFee
        };

        Assert.Equal(expected, product.CostPrice);
    }

    [Fact]
    public void Profit_ShouldEqual_ListingPrice_Minus_CostPrice()
    {
        var product = new Product
        {
            Name = "Test",
            UnitPrice = 100,
            CurrencyId = 1,
            ExchangeRate = 31.5m,
            Discount = 0.9m,
            ListingPrice = 5000,
            CommissionFee = 15
        };

        // CostPrice = 100*31.5*0.9 + 5000*(15/100) = 2835 + 750 = 3585
        Assert.Equal(1415m, product.Profit);
    }

    [Fact]
    public void ProfitMargin_Calculation()
    {
        var product = new Product
        {
            Name = "Test",
            UnitPrice = 100,
            CurrencyId = 1,
            ExchangeRate = 31.5m,
            Discount = 0.9m,
            ListingPrice = 5000,
            CommissionFee = 15
        };

        // Profit=1415, ProfitMargin = (1415/5000)*100 = 28.3
        Assert.Equal(28.3m, product.ProfitMargin);
    }

    [Fact]
    public void ProfitMargin_ReturnsZero_WhenListingPriceIsZero()
    {
        var product = new Product
        {
            Name = "Test",
            UnitPrice = 100,
            CurrencyId = 1,
            ExchangeRate = 1m
        };

        Assert.Equal(0m, product.ProfitMargin);
    }

    [Fact]
    public void Discount_DefaultsTo_One()
    {
        var product = new Product
        {
            Name = "Test",
            UnitPrice = 100,
            CurrencyId = 1,
            ExchangeRate = 1m
        };

        Assert.Equal(1.0m, product.Discount);
    }
}
