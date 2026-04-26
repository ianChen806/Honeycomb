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

### Requirement: User can open and close product search overlay
The system SHALL provide a floating search overlay within each category tab that can be opened with `Ctrl+F` and closed with `Esc` or a close button.

#### Scenario: Open search overlay with Ctrl+F
- **WHEN** user is viewing a category tab and presses `Ctrl+F`
- **THEN** system displays a floating search overlay in the top-right corner of the product list and moves keyboard focus into the search text box

#### Scenario: Close search overlay with Esc
- **WHEN** the search overlay is open and user presses `Esc` while focus is in the search box
- **THEN** system hides the search overlay and returns keyboard focus to the product grid

#### Scenario: Close search overlay with close button
- **WHEN** the search overlay is open and user clicks the close (✕) button
- **THEN** system hides the search overlay and returns keyboard focus to the product grid

#### Scenario: Reopen search overlay preserves previous query
- **WHEN** user opens search, types a query, closes the overlay with `Esc`, then presses `Ctrl+F` again
- **THEN** system reopens the overlay with the previous query text retained in the search box

### Requirement: System filters product matches by name (case-insensitive)
The system SHALL match products in the current category whose `Name` contains the search query as a case-insensitive substring. Search is scoped to the active category tab only.

#### Scenario: Substring match on product name
- **WHEN** the current category contains products "Widget A", "Widget B", "Other" and user types "Widget" in the search box
- **THEN** system identifies "Widget A" and "Widget B" as matches in the order they appear in the product list

#### Scenario: Case-insensitive match
- **WHEN** the current category contains a product "Widget A" and user types "widget" in the search box
- **THEN** system identifies "Widget A" as a match

#### Scenario: No matches
- **WHEN** user types a query that does not match any product name in the current category
- **THEN** system displays match count "0/0" and does not change the product grid selection

#### Scenario: Empty query
- **WHEN** user clears the search query to an empty string
- **THEN** system clears all matches and displays match count "0/0"

#### Scenario: Search scope limited to current tab
- **WHEN** user is viewing category "A" with one matching product and category "B" also contains a matching product
- **THEN** system only counts and displays the match from category "A"

### Requirement: System highlights and scrolls to current match
The system SHALL highlight the current match by setting it as the data grid's selected item and scrolling it into view.

#### Scenario: First match highlighted on query input
- **WHEN** the search overlay is open and user types a query that produces at least one match
- **THEN** system selects the first matching product in the data grid and scrolls it into view

#### Scenario: Highlight follows match navigation
- **WHEN** the user navigates between matches via `Enter` or `Shift+Enter`
- **THEN** system updates the selected item in the data grid to the new current match and scrolls it into view

### Requirement: User can navigate between matches with Enter and Shift+Enter
The system SHALL allow the user to advance to the next match with `Enter` and to the previous match with `Shift+Enter`. Navigation SHALL wrap around at both ends of the match list.

#### Scenario: Enter advances to next match
- **WHEN** there are 3 matches with current index 1 and user presses `Enter`
- **THEN** system moves current match to index 2 and updates the count display to "2/3"

#### Scenario: Enter wraps from last to first match
- **WHEN** the current match is the last one in the match list and user presses `Enter`
- **THEN** system moves current match back to the first one in the list

#### Scenario: Shift+Enter wraps from first to last match
- **WHEN** the current match is the first one in the match list and user presses `Shift+Enter`
- **THEN** system moves current match to the last one in the list

#### Scenario: Navigation with no matches is a no-op
- **WHEN** there are zero matches and user presses `Enter` or `Shift+Enter`
- **THEN** system does not change selection or count

### Requirement: System displays match count
The system SHALL display the current match position and total match count in the form `<current>/<total>` (1-based) inside the search overlay.

#### Scenario: Count format with matches
- **WHEN** there are 5 matches and the current match index is 2 (0-based)
- **THEN** system displays "3/5"

#### Scenario: Count format with no matches
- **WHEN** there are zero matches (either because of an empty query or no match found)
- **THEN** system displays "0/0"

### Requirement: Product list reload resets search state
The system SHALL clear the search query and match state whenever the product list is reloaded (after add, delete, move, or external data refresh).

#### Scenario: Adding a product clears search
- **WHEN** the user has an active search query and adds a new product
- **THEN** system clears the search query, sets match count to "0/0", and removes the highlighted match selection

#### Scenario: Deleting a product clears search
- **WHEN** the user has an active search query and deletes one or more selected products
- **THEN** system clears the search query and resets match count to "0/0"

#### Scenario: Moving products to another category clears search
- **WHEN** the user has an active search query and moves products to another category
- **THEN** system clears the search query in the source tab and resets match count to "0/0"
