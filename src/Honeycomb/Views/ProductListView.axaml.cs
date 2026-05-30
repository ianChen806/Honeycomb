using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is ProductListViewModel vm)
        {
            vm.MatchScrollRequested += OnMatchScrollRequested;
            vm.PropertyChanged += OnVmPropertyChanged;
            vm.OrderedProductsProvider = GetOrderedProducts;
            ProductGrid.Sorting += OnGridSorting;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is ProductListViewModel vm)
        {
            vm.MatchScrollRequested -= OnMatchScrollRequested;
            vm.PropertyChanged -= OnVmPropertyChanged;
            vm.OrderedProductsProvider = null;
            ProductGrid.Sorting -= OnGridSorting;
        }
        base.OnDetachedFromVisualTree(e);
    }

    private void OnMatchScrollRequested(Product product)
    {
        ProductGrid.SelectedItem = product;
        ProductGrid.ScrollIntoView(product, null);
    }

    private IEnumerable<Product> GetOrderedProducts()
    {
        if (ProductGrid.CollectionView is { } view)
        {
            return view.OfType<Product>().ToList();
        }
        return DataContext is ProductListViewModel vm
            ? vm.Products.ToList()
            : new List<Product>();
    }

    private void OnGridSorting(object? sender, DataGridColumnEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm) return;
        Dispatcher.UIThread.Post(vm.OnSortChanged);
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductListViewModel.IsSearchVisible)
            && sender is ProductListViewModel vm
            && vm.IsSearchVisible)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
            });
        }
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

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm) return;

        switch (e.Key)
        {
            case Key.Enter:
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    vm.PreviousMatch();
                else
                    vm.NextMatch();
                e.Handled = true;
                break;

            case Key.Escape:
                vm.CloseSearch();
                ProductGrid.Focus();
                e.Handled = true;
                break;
        }
    }

    private void OnCloseSearchClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProductListViewModel vm)
        {
            vm.CloseSearch();
            ProductGrid.Focus();
        }
    }

    private void OnPreviousMatchClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProductListViewModel vm) vm.PreviousMatch();
    }

    private void OnNextMatchClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProductListViewModel vm) vm.NextMatch();
    }

    private async void OnPickNewImageClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm) return;
        var stream = await PickImageStreamAsync();
        if (stream is null) return;
        await using (stream)
        {
            vm.SetNewImage(stream);
        }
    }

    private void OnClearNewImageClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProductListViewModel vm)
            vm.NewImageBytes = null;
    }

    private async void OnPickImageClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProductListViewModel vm) return;
        var stream = await PickImageStreamAsync();
        if (stream is null) return;
        await using (stream)
        {
            vm.AttachImageToSelected(stream);
        }
    }

    private void OnRemoveImageClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProductListViewModel vm)
            vm.RemoveImageFromSelected();
    }

    private async Task<Stream?> PickImageStreamAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "選擇商品圖片",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("圖片")
                {
                    Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.webp", "*.bmp" }
                }
            }
        });

        if (files.Count == 0) return null;
        return await files[0].OpenReadAsync();
    }
}
