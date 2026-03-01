namespace Honeycomb.Models;

public sealed class Currency
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
}
