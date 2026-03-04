## MODIFIED Requirements

### Requirement: User can add a product
The system SHALL allow users to add a new product with the following fields: name (string, required), extra cost (decimal, optional, default 0, unit NT), unit price (decimal, required), currency (selected from configured currencies, required), exchange rate (decimal, required), and discount (decimal, optional, default 1.0).

#### Scenario: Add product with all fields
- **WHEN** user fills in name="Widget A", extra cost=50, unit price=100, currency="USD", exchange rate=31.5, discount=0.9 and clicks "Add"
- **THEN** system saves the product and it appears in the product list

#### Scenario: Add product with default extra cost
- **WHEN** user fills in required fields and leaves extra cost empty
- **THEN** system saves the product with extra cost=0

#### Scenario: Add product with missing required fields
- **WHEN** user attempts to add a product without filling in all required fields
- **THEN** system displays validation errors and does not save the product

### Requirement: System auto-calculates cost price
The system SHALL automatically calculate cost price as: `UnitPrice × ExchangeRate × Discount + ListingPrice × (CommissionFee / 100) + ExtraCost`. ExtraCost is in NT and SHALL NOT be multiplied by exchange rate.

#### Scenario: Cost price calculation with extra cost
- **WHEN** product has unit price=100, exchange rate=31.5, discount=0.9, listing price=5000, commission fee=15, extra cost=200
- **THEN** system displays cost price as 100×31.5×0.9 + 5000×(15/100) + 200 = 2835 + 750 + 200 = 3785

#### Scenario: Cost price with zero extra cost
- **WHEN** product has unit price=100, exchange rate=31.5, discount=0.9, listing price=5000, commission fee=15, extra cost=0
- **THEN** system displays cost price as 3585

### Requirement: User can view product list
The system SHALL display all saved products in a data grid showing: name, extra cost (NT), unit price, currency code, exchange rate, discount, listing price, commission fee (%), cost price, profit, profit margin (%), and created date. The data grid SHALL use alternating row background colors (default background and a subtle gray) for readability.

#### Scenario: View product list with alternating rows
- **WHEN** user views the product list
- **THEN** odd rows display with the default background and even rows display with a subtle gray background

#### Scenario: Profit margin displays with percent sign
- **WHEN** a product has profit margin of 25.5
- **THEN** the column displays "25.50%"

## REMOVED Requirements

### Requirement: System auto-calculates total price
**Reason**: Total price (Quantity × UnitPrice × ExchangeRate × Discount) is no longer applicable. Quantity has been replaced by ExtraCost which serves a different purpose (additional fixed costs in NT).
**Migration**: ExtraCost is incorporated into the CostPrice formula instead.
