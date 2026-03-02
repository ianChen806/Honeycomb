## ADDED Requirements

### Requirement: User can add a product
The system SHALL allow users to add a new product with the following fields: name (string, required), quantity (integer, required), unit price (decimal, required), currency (selected from configured currencies, required), exchange rate (decimal, required), and discount (decimal, optional, default 1.0).

#### Scenario: Add product with all fields
- **WHEN** user fills in name="Widget A", quantity=10, unit price=100, currency="USD", exchange rate=31.5, discount=0.9 and clicks "Add"
- **THEN** system saves the product and it appears in the product list

#### Scenario: Add product with default discount
- **WHEN** user fills in required fields and leaves discount empty
- **THEN** system saves the product with discount=1.0 (no discount)

#### Scenario: Add product with missing required fields
- **WHEN** user attempts to add a product without filling in all required fields
- **THEN** system displays validation errors and does not save the product

### Requirement: System auto-calculates cost price
The system SHALL automatically calculate cost price as: `UnitPrice × Discount`.

#### Scenario: Cost price calculation
- **WHEN** user enters unit price=100 and discount=0.9
- **THEN** system displays cost price as 90

#### Scenario: Cost price with no discount
- **WHEN** user enters unit price=100 and discount=1.0
- **THEN** system displays cost price as 100

### Requirement: System auto-calculates total price
The system SHALL automatically calculate total price as: `Quantity × UnitPrice × ExchangeRate × Discount`.

#### Scenario: Total price calculation
- **WHEN** user enters quantity=10, unit price=100, exchange rate=31.5, discount=0.9
- **THEN** system displays total price as 28350

#### Scenario: Total price with no discount
- **WHEN** user enters quantity=5, unit price=200, exchange rate=1.0, discount=1.0
- **THEN** system displays total price as 1000

### Requirement: User can view product list
The system SHALL display all saved products in a data grid showing: name, quantity, unit price, currency code, exchange rate, discount, cost price, total price, and created date.

#### Scenario: View product list
- **WHEN** user navigates to the product list tab
- **THEN** system displays all products in a data grid with all columns

#### Scenario: Empty product list
- **WHEN** no products have been added
- **THEN** system displays an empty data grid

### Requirement: User can delete a product
The system SHALL allow users to delete a product from the list.

#### Scenario: Delete a product
- **WHEN** user selects a product and clicks "Delete"
- **THEN** system removes the product from the database and the list updates

### Requirement: Product records exchange rate at creation time
The system SHALL store the exchange rate entered at the time of product creation. This rate is a historical record and does not change after creation.

#### Scenario: Exchange rate is persisted
- **WHEN** user adds a product with exchange rate=31.5
- **THEN** the stored product record contains exchange rate=31.5 regardless of future rate changes
