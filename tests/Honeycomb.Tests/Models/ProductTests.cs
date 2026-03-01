using Honeycomb.Models;

namespace Honeycomb.Tests.Models;

public class ProductTests
{
    [Theory]
    [InlineData(100, 0.9, 90)]
    [InlineData(100, 1.0, 100)]
    [InlineData(250, 0.8, 200)]
    [InlineData(50, 0.5, 25)]
    public void CostPrice_ShouldEqual_UnitPrice_Times_Discount(
        decimal unitPrice, decimal discount, decimal expected)
    {
        var product = new Product
        {
            Name = "Test",
            Quantity = 1,
            UnitPrice = unitPrice,
            CurrencyId = 1,
            ExchangeRate = 1m,
            Discount = discount
        };

        Assert.Equal(expected, product.CostPrice);
    }

    [Theory]
    [InlineData(10, 100, 31.5, 0.9, 28350)]
    [InlineData(5, 200, 1.0, 1.0, 1000)]
    [InlineData(1, 100, 30.0, 0.8, 2400)]
    [InlineData(3, 50, 4.5, 0.9, 607.5)]
    public void TotalPrice_ShouldEqual_Quantity_Times_UnitPrice_Times_ExchangeRate_Times_Discount(
        int quantity, decimal unitPrice, decimal exchangeRate, decimal discount, decimal expected)
    {
        var product = new Product
        {
            Name = "Test",
            Quantity = quantity,
            UnitPrice = unitPrice,
            CurrencyId = 1,
            ExchangeRate = exchangeRate,
            Discount = discount
        };

        Assert.Equal(expected, product.TotalPrice);
    }

    [Fact]
    public void Discount_DefaultsTo_One()
    {
        var product = new Product
        {
            Name = "Test",
            Quantity = 1,
            UnitPrice = 100,
            CurrencyId = 1,
            ExchangeRate = 1m
        };

        Assert.Equal(1.0m, product.Discount);
    }
}
