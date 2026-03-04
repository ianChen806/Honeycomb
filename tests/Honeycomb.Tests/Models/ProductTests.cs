using Honeycomb.Models;

namespace Honeycomb.Tests.Models;

public class ProductTests
{
    [Theory]
    [InlineData(100, 1, 0.9, 0, 15, 0, 90)]
    [InlineData(100, 31.5, 1.0, 500, 15, 0, 3225)]
    [InlineData(200, 0.22, 0.85, 1000, 10, 0, 137.4)]
    [InlineData(100, 31.5, 0.9, 5000, 15, 200, 3785)]
    [InlineData(100, 1, 1, 0, 15, 500, 600)]
    public void CostPrice_Calculation(
        decimal unitPrice, decimal exchangeRate, decimal discount,
        decimal listingPrice, decimal commissionFee, decimal extraCost, decimal expected)
    {
        var product = new Product
        {
            Name = "Test",
            UnitPrice = unitPrice,
            CurrencyId = 1,
            ExchangeRate = exchangeRate,
            Discount = discount,
            ListingPrice = listingPrice,
            CommissionFee = commissionFee,
            ExtraCost = extraCost
        };

        Assert.Equal(expected, product.CostPrice);
    }

    [Fact]
    public void ExtraCost_DefaultsTo_Zero()
    {
        var product = new Product
        {
            Name = "Test",
            UnitPrice = 100,
            CurrencyId = 1,
            ExchangeRate = 1m
        };

        Assert.Equal(0m, product.ExtraCost);
    }

    [Fact]
    public void ExtraCost_NotAffectedByExchangeRate()
    {
        var product1 = new Product
        {
            Name = "Test",
            UnitPrice = 0,
            CurrencyId = 1,
            ExchangeRate = 31.5m,
            ExtraCost = 100
        };

        var product2 = new Product
        {
            Name = "Test",
            UnitPrice = 0,
            CurrencyId = 1,
            ExchangeRate = 1m,
            ExtraCost = 100
        };

        // ExtraCost should contribute equally regardless of exchange rate
        Assert.Equal(product1.ExtraCost, product2.ExtraCost);
        // Both CostPrice should include ExtraCost = 100 (UnitPrice is 0)
        Assert.Equal(100m, product1.CostPrice);
        Assert.Equal(100m, product2.CostPrice);
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
            CommissionFee = 15,
            ExtraCost = 200
        };

        // CostPrice = 100*31.5*0.9 + 5000*(15/100) + 200 = 2835 + 750 + 200 = 3785
        Assert.Equal(1215m, product.Profit);
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
            CommissionFee = 15,
            ExtraCost = 200
        };

        // Profit=1215, ProfitMargin = (1215/5000)*100 = 24.3
        Assert.Equal(24.3m, product.ProfitMargin);
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
