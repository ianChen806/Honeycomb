using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Honeycomb.Models;
using Honeycomb.ViewModels;

namespace Honeycomb.Views;

public partial class ProductListView : UserControl
{
    public ProductListView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (ProductGrid.Columns.Count > 0)
        {
            ProductGrid.Columns[0].Sort(ListSortDirection.Ascending);
        }
    }

    private void OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Commit && DataContext is ProductListViewModel vm)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => vm.SaveProductChanges());
        }
    }

    private void OnBackgroundPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual visual) return;
        if (visual.FindAncestorOfType<DataGridRow>() is not null) return;
        if (visual.FindAncestorOfType<DataGridColumnHeader>() is not null) return;
        if (visual.FindAncestorOfType<Button>() is not null) return;
        if (visual.FindAncestorOfType<TextBox>() is not null) return;
        if (visual.FindAncestorOfType<NumericUpDown>() is not null) return;
        if (visual.FindAncestorOfType<ComboBox>() is not null) return;

        ProductGrid.SelectedItem = null;
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
