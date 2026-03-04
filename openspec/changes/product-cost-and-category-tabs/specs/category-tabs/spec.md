## ADDED Requirements

### Requirement: User can create a category
The system SHALL allow users to create a new category (大分類) by specifying a name. Each category appears as a separate Tab in the main window.

#### Scenario: Create a new category
- **WHEN** user clicks "add category" and enters name "日用品"
- **THEN** system creates a new category and a corresponding Tab appears in the main window

#### Scenario: Create category with duplicate name
- **WHEN** user attempts to create a category with a name that already exists
- **THEN** system displays an error and does not create the category

### Requirement: User can rename a category
The system SHALL allow users to rename an existing category. The Tab header updates to reflect the new name.

#### Scenario: Rename a category
- **WHEN** user renames category "日用品" to "生活用品"
- **THEN** the Tab header updates to "生活用品" and all products remain associated

#### Scenario: Rename to duplicate name
- **WHEN** user attempts to rename a category to a name that already exists
- **THEN** system displays an error and keeps the original name

### Requirement: User can delete a category
The system SHALL allow users to delete a category. Deletion requires confirmation. If the category contains products, the system SHALL require a second confirmation warning that products will become uncategorized.

#### Scenario: Delete empty category
- **WHEN** user deletes a category that has no products and confirms the first dialog
- **THEN** system removes the category and its Tab disappears

#### Scenario: Delete category with products - confirmed
- **WHEN** user deletes a category containing products, confirms the first dialog, and confirms the second warning dialog
- **THEN** system removes the category, its Tab disappears, and products' CategoryId is set to null

#### Scenario: Delete category with products - cancelled at second dialog
- **WHEN** user deletes a category containing products, confirms the first dialog, but cancels the second warning dialog
- **THEN** system does not delete the category

### Requirement: Each category has its own product Tab
The system SHALL display each category as a separate Tab. Each Tab contains its own product list showing only products belonging to that category. Products within a Tab can be added, edited, and deleted independently.

#### Scenario: View products in a category Tab
- **WHEN** user selects the "日用品" Tab
- **THEN** system displays only products with CategoryId matching "日用品"

#### Scenario: Add product in a category Tab
- **WHEN** user adds a product while in the "日用品" Tab
- **THEN** the product is saved with CategoryId set to the "日用品" category

### Requirement: Currency settings is a shared Tab
The system SHALL display the currency settings as a fixed Tab that is independent of categories. It SHALL always be visible regardless of which categories exist.

#### Scenario: Currency Tab is always present
- **WHEN** user has zero categories
- **THEN** the currency settings Tab is still visible

#### Scenario: Currency Tab is independent
- **WHEN** user deletes all categories
- **THEN** the currency settings Tab remains unaffected
