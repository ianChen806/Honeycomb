## ADDED Requirements

### Requirement: User can add a currency
The system SHALL allow users to add a new currency with a code (e.g., "USD", "JPY") and a display name (e.g., "美元", "日圓").

#### Scenario: Add a new currency
- **WHEN** user enters code="EUR" and name="歐元" and clicks "Add"
- **THEN** system saves the currency and it appears in the currency list

#### Scenario: Add currency with duplicate code
- **WHEN** user attempts to add a currency with a code that already exists
- **THEN** system displays an error and does not save the duplicate

#### Scenario: Add currency with missing fields
- **WHEN** user attempts to add a currency without filling in code or name
- **THEN** system displays validation errors and does not save

### Requirement: User can delete a currency
The system SHALL allow users to delete a currency only if no products are currently using it.

#### Scenario: Delete unused currency
- **WHEN** user selects a currency that has no associated products and clicks "Delete"
- **THEN** system removes the currency from the list

#### Scenario: Delete currency in use
- **WHEN** user selects a currency that is associated with one or more products and clicks "Delete"
- **THEN** system displays a warning that the currency is in use and does not delete it

### Requirement: User can view currency list
The system SHALL display all configured currencies showing code and name.

#### Scenario: View currency list
- **WHEN** user navigates to the currency settings tab
- **THEN** system displays all configured currencies

### Requirement: Currencies are available for product selection
The system SHALL provide the list of configured currencies as a dropdown selection when adding a product.

#### Scenario: Currency dropdown populated
- **WHEN** user opens the add product form
- **THEN** the currency dropdown contains all configured currencies
