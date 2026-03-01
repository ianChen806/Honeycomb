using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Honeycomb.Data;
using Honeycomb.Services;
using Honeycomb.ViewModels;
using Honeycomb.Views;
using Microsoft.EntityFrameworkCore;

namespace Honeycomb;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            var db = new AppDbContext();
            db.Database.Migrate();

            var excelExport = new ExcelExportService();

            var mainWindow = new MainWindow();

            async Task<string?> GetSaveFilePath()
            {
                var file = await mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "匯出 Excel",
                    DefaultExtension = "xlsx",
                    SuggestedFileName = $"{DateTime.Now:yyyyMMdd}_庫存清單",
                    FileTypeChoices = new List<FilePickerFileType>
                    {
                        new("Excel 檔案") { Patterns = new List<string> { "*.xlsx" } }
                    }
                });
                return file?.Path.LocalPath;
            }

            mainWindow.DataContext = new MainWindowViewModel(db, excelExport, GetSaveFilePath);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
