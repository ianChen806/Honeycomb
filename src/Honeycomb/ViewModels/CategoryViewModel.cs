using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Honeycomb.Data;
using Honeycomb.Models;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb.ViewModels;

public partial class CategoryViewModel : ViewModelBase
{
    private readonly AppDbContext _db;

    public event Action? CategoriesChanged;

    public ObservableCollection<Category> Categories { get; } = [];

    [ObservableProperty]
    private string _newCategoryName = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public CategoryViewModel(AppDbContext db)
    {
        _db = db;
        LoadCategories();
    }

    public void LoadCategories()
    {
        Categories.Clear();
        foreach (var category in _db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ToList())
        {
            Categories.Add(category);
        }
    }

    [RelayCommand]
    private void AddCategory()
    {
        ErrorMessage = string.Empty;

        var name = NewCategoryName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorMessage = "分類名稱為必填";
            return;
        }

        if (_db.Categories.Any(c => c.Name == name))
        {
            ErrorMessage = $"分類 '{name}' 已存在";
            return;
        }

        var maxSortOrder = _db.Categories.Any() ? _db.Categories.Max(c => c.SortOrder) : -1;
        _db.Categories.Add(new Category { Name = name, SortOrder = maxSortOrder + 1 });
        _db.SaveChanges();

        NewCategoryName = string.Empty;
        LoadCategories();
        CategoriesChanged?.Invoke();
    }

    public bool CategoryHasProducts(int categoryId)
    {
        return _db.Products.Any(p => p.CategoryId == categoryId);
    }

    public void DeleteCategory(int categoryId)
    {
        ErrorMessage = string.Empty;

        var entity = _db.Categories.Find(categoryId);
        if (entity is null) return;

        // Move products to default category before deleting
        var products = _db.Products.Where(p => p.CategoryId == categoryId).ToList();
        foreach (var product in products)
        {
            product.CategoryId = MainWindowViewModel.DefaultCategoryId;
        }

        _db.Categories.Remove(entity);
        _db.SaveChanges();

        LoadCategories();
        CategoriesChanged?.Invoke();
    }

    public void ReorderCategory(int categoryId, int newIndex)
    {
        var categories = _db.Categories.OrderBy(c => c.SortOrder).ToList();
        var item = categories.Find(c => c.Id == categoryId);
        if (item is null) return;

        categories.Remove(item);
        var clampedIndex = Math.Clamp(newIndex, 0, categories.Count);
        categories.Insert(clampedIndex, item);

        for (var i = 0; i < categories.Count; i++)
        {
            categories[i].SortOrder = i;
        }

        _db.SaveChanges();
        LoadCategories();
        CategoriesChanged?.Invoke();
    }

    public bool RenameCategory(int categoryId, string newName)
    {
        ErrorMessage = string.Empty;

        var trimmed = newName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            ErrorMessage = "分類名稱為必填";
            return false;
        }

        if (_db.Categories.Any(c => c.Name == trimmed && c.Id != categoryId))
        {
            ErrorMessage = $"分類 '{trimmed}' 已存在";
            return false;
        }

        var entity = _db.Categories.Find(categoryId);
        if (entity is null) return false;

        entity.Name = trimmed;
        _db.SaveChanges();

        LoadCategories();
        CategoriesChanged?.Invoke();
        return true;
    }
}
