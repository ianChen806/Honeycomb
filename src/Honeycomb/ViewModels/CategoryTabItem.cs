using CommunityToolkit.Mvvm.ComponentModel;

namespace Honeycomb.ViewModels;

public partial class CategoryTabItem : ObservableObject
{
    public int CategoryId { get; }

    [ObservableProperty]
    private string _header;

    public ProductListViewModel ProductList { get; }

    public CategoryTabItem(int categoryId, string header, ProductListViewModel productList)
    {
        CategoryId = categoryId;
        _header = header;
        ProductList = productList;
    }
}
