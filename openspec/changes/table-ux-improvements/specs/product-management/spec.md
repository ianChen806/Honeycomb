## MODIFIED Requirements

### Requirement: User can view product list
The system SHALL display all saved products in a data grid showing: name, unit price, currency code, exchange rate, extra cost, discount, listing price, commission fee (with `%` suffix in cell value), cost price, profit, profit margin (with `%` suffix), and created date.

#### Scenario: Commission fee displays with percent sign
- **WHEN** a product has commission fee=10
- **THEN** the DataGrid cell displays "10%"

#### Scenario: View product list
- **WHEN** user navigates to the product list tab
- **THEN** system displays all products in a data grid with all columns
