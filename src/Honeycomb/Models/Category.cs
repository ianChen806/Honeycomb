namespace Honeycomb.Models;

public sealed class Category
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
}
