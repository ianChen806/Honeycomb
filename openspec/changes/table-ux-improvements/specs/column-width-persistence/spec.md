## ADDED Requirements

### Requirement: DataGrid columns are user-resizable
The system SHALL allow users to drag DataGrid column borders to adjust column width.

#### Scenario: User drags column border to resize
- **WHEN** user drags a column header border in the DataGrid
- **THEN** the column width changes to match the drag position

### Requirement: Column widths are persisted per category tab
The system SHALL save each category tab's DataGrid column widths independently to `%LocalAppData%/Honeycomb/column-widths.json`.

#### Scenario: Save column widths on application close
- **WHEN** user adjusts column widths in category "預設" and closes the application
- **THEN** the column widths for category ID 1 are saved to `column-widths.json`

#### Scenario: Restore column widths on application start
- **WHEN** user opens the application and `column-widths.json` contains saved widths for category ID 1
- **THEN** the DataGrid in category "預設" tab restores the saved column widths

#### Scenario: Different tabs have independent widths
- **WHEN** user sets "商品名稱" column to 200px in tab A and 300px in tab B
- **THEN** each tab retains its own width when switching between tabs

### Requirement: Graceful fallback when no saved widths exist
The system SHALL use XAML default column widths when no saved widths are found for a category tab.

#### Scenario: First launch with no saved widths
- **WHEN** `column-widths.json` does not exist or does not contain the category ID
- **THEN** the DataGrid uses the default column widths defined in XAML

#### Scenario: Corrupted settings file
- **WHEN** `column-widths.json` is corrupted or unreadable
- **THEN** the system silently ignores the error and uses default column widths
