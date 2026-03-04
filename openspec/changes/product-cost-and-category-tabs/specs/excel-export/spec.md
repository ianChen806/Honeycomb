## MODIFIED Requirements

### Requirement: Excel file contains complete product information
The exported Excel file SHALL include columns: product name, extra cost (NT), unit price, currency code, exchange rate, discount, listing price, commission fee (%), cost price, profit, profit margin (%), and created date. The profit margin column SHALL display values with a `%` suffix.

#### Scenario: Excel column completeness
- **WHEN** user exports products to Excel
- **THEN** the file contains a header row with all specified columns and one data row per product

#### Scenario: Profit margin format in Excel
- **WHEN** a product with profit margin 25.5 is exported
- **THEN** the Excel cell displays "25.50%"
