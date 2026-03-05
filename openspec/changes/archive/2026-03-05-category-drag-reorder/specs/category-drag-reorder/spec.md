## ADDED Requirements

### Requirement: Category has a sort order
Each Category SHALL have a `SortOrder` integer property that determines its display position in the Tab bar. SortOrder values MUST be unique and contiguous (0, 1, 2, ...).

#### Scenario: New category gets last position
- **WHEN** a new category is added
- **THEN** its SortOrder SHALL be set to the current maximum SortOrder + 1

#### Scenario: Categories displayed in sort order
- **WHEN** the category tabs are built
- **THEN** tabs SHALL be ordered by SortOrder ascending (not by Id or Name)

### Requirement: User can drag-and-drop category tabs to reorder
The system SHALL allow the user to reorder category tabs by dragging a tab and dropping it at a new position within the tab bar.

#### Scenario: Drag tab to a new position
- **WHEN** user drags a category tab and drops it at a different position
- **THEN** the tab SHALL move to the drop position
- **AND** all affected tabs SHALL have their SortOrder updated to reflect the new order

#### Scenario: Drop at same position
- **WHEN** user drags a tab and drops it at its original position
- **THEN** no changes SHALL occur

#### Scenario: Visual feedback during drag
- **WHEN** user is dragging a category tab
- **THEN** the cursor SHALL change to indicate a drag operation is in progress

### Requirement: Reordered positions are persisted
The system SHALL persist the updated SortOrder values to the database immediately after a drag-and-drop reorder operation completes.

#### Scenario: Order survives restart
- **WHEN** user reorders category tabs and restarts the application
- **THEN** the category tabs SHALL appear in the previously saved order

#### Scenario: Persistence on drop
- **WHEN** a drag-and-drop reorder completes
- **THEN** all Category SortOrder values SHALL be saved to the database in a single transaction

### Requirement: Migration assigns initial sort order
The EF Core migration SHALL assign initial SortOrder values to existing categories.

#### Scenario: Existing categories get sort order
- **WHEN** the migration runs on an existing database
- **THEN** each existing category SHALL receive a SortOrder based on its current Id order (lowest Id = 0, next = 1, etc.)
