## ADDED Requirements

### Requirement: User can move selected products to another category
The system SHALL provide a "移動到分類" button next to the existing "刪除選取的商品" button. Clicking it SHALL open a dialog to select the target category.

#### Scenario: Move single product to another category
- **WHEN** user selects one product, clicks "移動到分類", selects target category "電子產品", and confirms
- **THEN** the product's CategoryId is updated to "電子產品" category, the product disappears from the current tab, and changes are saved to database

#### Scenario: Move multiple products to another category
- **WHEN** user selects 3 products, clicks "移動到分類", selects a target category, and confirms
- **THEN** all 3 products are moved to the target category in a single database operation

#### Scenario: No products selected
- **WHEN** user clicks "移動到分類" without selecting any products
- **THEN** the system does nothing (button click is ignored)

### Requirement: Move category dialog shows available categories
The dialog SHALL display all categories EXCEPT the current category in a ComboBox for selection.

#### Scenario: Dialog excludes current category
- **WHEN** user is on the "預設" tab and opens the move dialog
- **THEN** the dialog ComboBox shows all categories except "預設"

#### Scenario: User cancels the dialog
- **WHEN** user opens the move dialog and clicks "取消" or closes the dialog
- **THEN** no products are moved and the product list remains unchanged

### Requirement: Current tab refreshes after move
The system SHALL refresh the current tab's product list after a successful move operation.

#### Scenario: Products removed from current view after move
- **WHEN** user moves 2 products from "預設" to "電子產品"
- **THEN** the "預設" tab's DataGrid no longer shows those 2 products
