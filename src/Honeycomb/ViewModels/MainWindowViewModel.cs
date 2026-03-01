using Honeycomb.Data;
using Honeycomb.Services;

namespace Honeycomb.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public CurrencySettingsViewModel CurrencySettings { get; }
    public ProductListViewModel ProductList { get; }

    public MainWindowViewModel(AppDbContext db, ExcelExportService excelExport, System.Func<System.Threading.Tasks.Task<string?>> getSaveFilePath)
    {
        CurrencySettings = new CurrencySettingsViewModel(db);
        ProductList = new ProductListViewModel(db, excelExport, getSaveFilePath);

        CurrencySettings.CurrenciesChanged += () => ProductList.LoadData();
    }
}
