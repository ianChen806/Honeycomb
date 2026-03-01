using System;

namespace Honeycomb.Models;

public sealed class Product
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required int CurrencyId { get; init; }
    public Currency? Currency { get; init; }
    public required decimal ExchangeRate { get; init; }
    public decimal Discount { get; init; } = 1.0m;
    public decimal CostPrice => UnitPrice * Discount;
    public decimal TotalPrice => Quantity * UnitPrice * ExchangeRate * Discount;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}
