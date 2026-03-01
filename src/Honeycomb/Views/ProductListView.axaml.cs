using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Honeycomb.Models;
using Honeycomb.ViewModels;

namespace Honeycomb.Views;

public partial class ProductListView : UserControl
{
    public ProductListView()
    {
        InitializeComponent();
    }

    private void OnDeleteClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm)
            return;

        var selected = ProductGrid.SelectedItems
            .OfType<Product>()
            .ToList();

        if (selected.Count > 0)
        {
            vm.DeleteProducts(selected);
        }
    }
}
