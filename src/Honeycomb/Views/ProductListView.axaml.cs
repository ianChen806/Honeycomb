using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Honeycomb.Models;
using Honeycomb.Services;
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

        RestoreColumnWidths();
    }

    private void RestoreColumnWidths()
    {
        if (DataContext is not ProductListViewModel vm)
            return;

        var widths = ColumnWidthService.Load(vm.CategoryId);
        foreach (var col in ProductGrid.Columns)
        {
            if (col.Header is string header && widths.TryGetValue(header, out var width) && width >= 10)
            {
                col.Width = new DataGridLength(width);
            }
        }
    }

    public void SaveColumnWidths()
    {
        if (DataContext is not ProductListViewModel vm)
            return;

        var widths = new Dictionary<string, double>();
        foreach (var col in ProductGrid.Columns)
        {
            if (col.Header is string header)
            {
                widths[header] = col.ActualWidth;
            }
        }

        ColumnWidthService.Save(vm.CategoryId, widths);
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

    private async void OnMoveCategoryClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm)
            return;

        var selected = ProductGrid.SelectedItems
            .OfType<Product>()
            .ToList();

        if (selected.Count == 0)
            return;

        var categories = vm.GetOtherCategories();
        if (categories.Count == 0)
            return;

        var parentWindow = this.FindAncestorOfType<Window>();
        if (parentWindow is null)
            return;

        var dialog = new MoveCategoryDialog(categories);
        await dialog.ShowDialog(parentWindow);

        if (dialog.SelectedCategory is { } target)
        {
            vm.MoveProducts(selected, target.Id);
        }
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e) { }
    private void OnCloseSearchClicked(object? sender, RoutedEventArgs e) { }
}
