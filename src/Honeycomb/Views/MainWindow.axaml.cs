using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Honeycomb.ViewModels;

namespace Honeycomb.Views;

public partial class MainWindow : Window
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Honeycomb", "window.json");

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += (_, _) =>
        {
            SaveWindowSize();
            SaveAllColumnWidths();
        };
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        RestoreWindowSize();

        if (DataContext is MainWindowViewModel vm)
        {
            vm.CategoryTabs.CollectionChanged += OnCategoryTabsChanged;
            RebuildTabs(vm);
        }
    }

    private void OnCategoryTabsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
            RebuildTabs(vm);
    }

    private void RebuildTabs(MainWindowViewModel vm)
    {
        MainTabControl.Items.Clear();

        // Category tabs (includes "預設" from seed data)
        foreach (var categoryTab in vm.CategoryTabs)
        {
            var tabItem = new TabItem
            {
                Header = categoryTab.Header,
                Content = new ProductListView { DataContext = categoryTab.ProductList },
                Tag = categoryTab
            };

            // Right-click context menu (skip default category for delete)
            var menu = new ContextMenu();

            var renameItem = new MenuItem { Header = "重新命名" };
            renameItem.Click += (_, _) => OnRenameCategoryFromMenu(categoryTab);
            menu.Items.Add(renameItem);

            if (categoryTab.CategoryId != MainWindowViewModel.DefaultCategoryId)
            {
                var deleteItem = new MenuItem { Header = "刪除分類" };
                deleteItem.Click += (_, _) => OnDeleteCategoryFromMenu(categoryTab);
                menu.Items.Add(deleteItem);
            }

            tabItem.ContextMenu = menu;
            MainTabControl.Items.Add(tabItem);
        }

        // Fixed currency settings tab
        var currencyTab = new TabItem
        {
            Header = "幣別設定",
            Content = new CurrencySettingsView { DataContext = vm.CurrencySettings }
        };
        MainTabControl.Items.Add(currencyTab);

        if (MainTabControl.Items.Count > 0)
            MainTabControl.SelectedIndex = 0;
    }

    private CategoryTabItem? GetSelectedCategoryTab()
    {
        if (MainTabControl.SelectedItem is TabItem { Tag: CategoryTabItem tab })
            return tab;
        return null;
    }

    private async void OnAddCategoryClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var name = await ShowInputDialog("新增分類", "請輸入分類名稱：", "");
        if (string.IsNullOrWhiteSpace(name)) return;

        vm.CategoryManager.NewCategoryName = name;
        vm.CategoryManager.AddCategoryCommand.Execute(null);
    }

    private async void OnDeleteCategoryFromMenu(CategoryTabItem tab)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var hasProducts = vm.CategoryManager.CategoryHasProducts(tab.CategoryId);

        var result = await ShowConfirmDialog("確認刪除", $"確定要刪除分類「{tab.Header}」嗎？");
        if (!result) return;

        if (hasProducts)
        {
            var result2 = await ShowConfirmDialog("注意",
                $"分類「{tab.Header}」底下還有商品，刪除後這些商品將移至「預設」分類。確定要繼續嗎？");
            if (!result2) return;
        }

        vm.CategoryManager.DeleteCategory(tab.CategoryId);
    }

    private async void OnRenameCategoryFromMenu(CategoryTabItem tab)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        var newName = await ShowInputDialog("重新命名分類", "請輸入新的分類名稱：", tab.Header);
        if (string.IsNullOrWhiteSpace(newName)) return;

        vm.CategoryManager.RenameCategory(tab.CategoryId, newName);
    }

    private async Task<bool> ShowConfirmDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var result = false;

        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 16 };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });

        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };

        var okBtn = new Button { Content = "確定" };
        okBtn.Click += (_, _) => { result = true; dialog.Close(); };

        var cancelBtn = new Button { Content = "取消" };
        cancelBtn.Click += (_, _) => dialog.Close();

        buttons.Children.Add(okBtn);
        buttons.Children.Add(cancelBtn);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        await dialog.ShowDialog(this);
        return result;
    }

    private async Task<string?> ShowInputDialog(string title, string message, string defaultValue)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        string? result = null;

        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = message });

        var input = new TextBox { Text = defaultValue };
        panel.Children.Add(input);

        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };

        var okBtn = new Button { Content = "確定" };
        okBtn.Click += (_, _) => { result = input.Text; dialog.Close(); };

        var cancelBtn = new Button { Content = "取消" };
        cancelBtn.Click += (_, _) => dialog.Close();

        buttons.Children.Add(okBtn);
        buttons.Children.Add(cancelBtn);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        await dialog.ShowDialog(this);
        return result;
    }

    private TabItem? _previousTab;

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MainTabControl is null) return;

        if (_previousTab?.Content is ProductListView prevView)
        {
            prevView.SaveColumnWidths();
        }

        _previousTab = MainTabControl.SelectedItem as TabItem;
    }

    private void SaveAllColumnWidths()
    {
        foreach (var item in MainTabControl.Items.OfType<TabItem>())
        {
            if (item.Content is ProductListView view)
            {
                view.SaveColumnWidths();
            }
        }
    }

    private void SaveWindowSize()
    {
        if (WindowState == WindowState.Minimized)
            return;

        var settings = new WindowSettings
        {
            Width = Width,
            Height = Height,
            IsMaximized = WindowState == WindowState.Maximized
        };

        try
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings));
        }
        catch
        {
            // Ignore save failures
        }
    }

    private void RestoreWindowSize()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return;

            var settings = JsonSerializer.Deserialize<WindowSettings>(File.ReadAllText(SettingsPath));
            if (settings is null)
                return;

            Width = settings.Width;
            Height = settings.Height;

            if (settings.IsMaximized)
                WindowState = WindowState.Maximized;
        }
        catch
        {
            // Ignore restore failures
        }
    }

    private sealed class WindowSettings
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsMaximized { get; set; }
    }
}
