## 1. Project Setup

- [x] 1.1 Create Avalonia app project with `dotnet new avalonia.app` targeting .NET 10
- [x] 1.2 Create solution file and organize project structure (Models, Data, ViewModels, Views, Services folders)
- [x] 1.3 Add NuGet packages: CommunityToolkit.Mvvm, Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.Design, ClosedXML
- [x] 1.4 Create test project with xUnit and add to solution

## 2. Data Layer

- [x] 2.1 Create Currency model (Id, Code, Name)
- [x] 2.2 Create Product model (Id, Name, Quantity, UnitPrice, CurrencyId, ExchangeRate, Discount, CostPrice, TotalPrice, CreatedAt)
- [x] 2.3 Create AppDbContext with DbSet<Currency> and DbSet<Product>, configure SQLite connection
- [x] 2.4 Create and apply EF Core initial migration

## 3. Currency Settings Feature

- [x] 3.1 Create CurrencySettingsViewModel with add/delete commands and currency list
- [x] 3.2 Create CurrencySettingsView with currency list, add form (code + name), and delete button
- [x] 3.3 Implement validation: required fields, duplicate code prevention
- [x] 3.4 Implement delete guard: prevent deletion of currencies in use by products

## 4. Product Management Feature

- [x] 4.1 Create ProductListViewModel with product list, add/delete commands, and auto-calculation
- [x] 4.2 Implement cost price auto-calculation (UnitPrice × Discount) and total price auto-calculation (Quantity × UnitPrice × ExchangeRate × Discount)
- [x] 4.3 Create ProductListView with DataGrid, add product form (name, quantity, unit price, currency dropdown, exchange rate, discount), and delete button
- [x] 4.4 Implement validation: required fields, numeric constraints, currency selection

## 5. Main Window & Navigation

- [x] 5.1 Create MainWindowViewModel with tab navigation
- [x] 5.2 Create MainWindow with TabControl containing ProductListView and CurrencySettingsView tabs
- [x] 5.3 Wire up DI and DbContext in App.axaml.cs / Program.cs

## 6. Excel Export Feature

- [x] 6.1 Create ExcelExportService using ClosedXML with columns: name, quantity, unit price, currency, exchange rate, discount, cost price, total price, created date
- [x] 6.2 Add "Export Excel" button to ProductListView with SaveFileDialog
- [x] 6.3 Handle empty product list case (show message instead of exporting)

## 7. Testing

- [x] 7.1 Write unit tests for cost price and total price calculations
- [x] 7.2 Write unit tests for ExcelExportService
- [x] 7.3 Write unit tests for currency deletion guard logic
- [x] 7.4 Verify test coverage ≥ 80%
