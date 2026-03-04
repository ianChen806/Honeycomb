using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Honeycomb.Models;

namespace Honeycomb.Views;

public partial class MoveCategoryDialog : Window
{
    public Category? SelectedCategory { get; private set; }

    public MoveCategoryDialog()
    {
        InitializeComponent();
    }

    public MoveCategoryDialog(IEnumerable<Category> categories) : this()
    {
        CategoryComboBox.ItemsSource = categories;
    }

    private void OnOkClicked(object? sender, RoutedEventArgs e)
    {
        if (CategoryComboBox.SelectedItem is Category selected)
        {
            SelectedCategory = selected;
            Close();
        }
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
