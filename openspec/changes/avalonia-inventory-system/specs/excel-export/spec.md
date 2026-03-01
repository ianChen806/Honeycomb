## ADDED Requirements

### Requirement: User can export products to Excel
The system SHALL allow users to export the complete product list to an Excel (.xlsx) file.

#### Scenario: Export all products
- **WHEN** user clicks "Export Excel" and selects a save location
- **THEN** system generates an .xlsx file containing all products at the chosen path

#### Scenario: Export with no products
- **WHEN** user clicks "Export Excel" but no products exist
- **THEN** system displays a message indicating there are no products to export

### Requirement: Excel file contains complete product information
The exported Excel file SHALL include columns: product name, quantity, unit price, currency code, exchange rate, discount, cost price, total price, and created date.

#### Scenario: Excel column completeness
- **WHEN** user exports products to Excel
- **THEN** the file contains a header row with all specified columns and one data row per product

### Requirement: User chooses save location
The system SHALL present a save file dialog for the user to choose the export file location and name.

#### Scenario: Save file dialog
- **WHEN** user clicks "Export Excel"
- **THEN** system opens a save file dialog with default filename and .xlsx filter
