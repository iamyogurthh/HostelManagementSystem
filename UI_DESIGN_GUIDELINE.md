# Hostel Management System UI Design Guideline

## 1. Purpose

This document defines the visual and interaction system for the Hostel Management System WPF application. It is intended for developers, designers, and reviewers who add or modify screens while keeping the existing application coherent.

The guideline is based on the current implementation in:

- `MainWindow.xaml`
- `Views/`
- `Styles/`
- `Styles/Themes/LightTheme.xaml`
- `Styles/Themes/DarkTheme.xaml`
- `Resources/DataTemplates.xaml`
- `ViewModels/`

The goal is a calm, information-dense administrative interface that works well for hostel staff managing buildings, rooms, residents, academic records, images, and backups.

## 2. Product Design Direction

### 2.1 Design keywords

The visual language should feel:

- **Clear**: users can immediately identify the current screen, action, and status.
- **Structured**: hierarchy follows building → floor → room → student.
- **Trustworthy**: destructive actions require confirmation and important data is presented without visual noise.
- **Efficient**: common administrative tasks require few clicks and support keyboard input where practical.
- **Localized**: Myanmar-language content must remain readable and visually balanced beside English labels and technical terms.
- **Consistent**: shared resources and component styles should be used instead of one-off styling.

### 2.2 Primary user priorities

The interface should optimize for these tasks, in order:

1. Understand current occupancy and resident status.
2. Locate a building, floor, room, or resident quickly.
3. Add or update resident and infrastructure information.
4. Review academic and identity information safely.
5. Export or import hostel data without ambiguity.

## 3. Application Shell

The application uses a fixed two-column shell:

```text
┌──────────────────────┬──────────────────────────────────────────┐
│  240 px sidebar      │  Main content area                       │
│                      │                                          │
│  Logo                │  Page header                             │
│  Dashboard           │  Cards, forms, tables, or detail content │
│  Residents           │                                          │
│  Buildings           │                                          │
│  Data transfer       │                                          │
│                      │                                          │
│  Settings            │                                          │
└──────────────────────┴──────────────────────────────────────────┘
```

### 3.1 Sidebar

The sidebar is `240` device-independent pixels wide and uses `CardBackground`. It should remain visually stable while the content area changes.

Rules:

- Keep the application logo centered at the top.
- Keep top-level navigation in one consistent order:
  1. Dashboard
  2. Residents
  3. Buildings
  4. Data transfer
  5. Settings
- Use the existing `SidebarItemStyle` for every top-level item.
- Use one icon per item and keep icon geometry visually similar.
- Use the active state to communicate location; do not rely on text alone.
- Keep settings visually separated at the bottom of the sidebar.
- Do not add long descriptions or secondary controls to sidebar items.

### 3.2 Content area

The content area uses `WindowBackground` and should:

- Provide approximately `20` pixels of outer content spacing where the view does not already define its own layout.
- Use a vertical `ScrollViewer` for content that may exceed the window height.
- Keep the main page title near the upper-left corner.
- Place the primary action near the title when the action applies to the whole screen.
- Avoid horizontal scrolling except for genuinely wide data grids.

### 3.3 Loading overlay

Long-running database, image, import, and export operations use the shared loading state.

When busy:

- Disable or visually block conflicting actions.
- Show a short localized status message.
- Keep the current page visible behind the overlay when possible.
- Always clear the overlay after success or failure.

Use `LoadingHelper.ExecuteWithLoadingAsync` rather than implementing a separate spinner pattern in a view model.

## 4. Layout System

### 4.1 Spacing scale

Use a small spacing scale rather than arbitrary values:

| Token | Use |
|---:|---|
| 4 | Small separation between label and supporting text |
| 6 | Label-to-control spacing |
| 8 | Card gaps, compact control spacing |
| 10 | Sidebar item and compact form spacing |
| 12 | Input padding and small internal spacing |
| 16 | Standard card padding, section spacing, header-to-content spacing |
| 20 | Main content margin and large card padding |
| 24 | Major section separation |
| 32 | Large screen-level separation |

Existing views commonly use `8`, `10`, `16`, and `20`; new UI should use these values whenever possible.

### 4.2 Standard dimensions

| Element | Recommended size |
|---|---:|
| Sidebar | 240 px wide |
| Page title | 24 px |
| Page subtitle | 14 px |
| Standard button height | 40–48 px |
| Search input height | 48 px |
| Compact combo box height | 38 px |
| Standard body text | 14 px |
| Supporting text | 12–13 px |
| KPI value | 24–26 px |
| Icon size | 18–24 px |
| Card corner radius | 8–10 px |
| Input corner radius | 6 px |

Use `Height="48"`, not a string with a unit suffix such as `Height="48px"`, in new WPF XAML unless the existing control specifically requires otherwise.

### 4.3 Grid and alignment rules

- Use `Grid` for two-dimensional alignment and form rows.
- Use `StackPanel` for short linear groups.
- Use `ColumnDefinition Width="*"` for flexible content and `Auto` for actions.
- Keep action buttons aligned on a common baseline.
- Right-align compact actions where users expect them, such as page-level Add buttons.
- Avoid deeply nested `StackPanel` layouts when a grid would make alignment clearer.

## 5. Color System

All colors should be accessed through dynamic resource keys when they represent application-level meaning. Do not hard-code theme-dependent colors in views.

### 5.1 Semantic resource keys

| Resource | Light theme | Dark theme | Usage |
|---|---|---|---|
| `WindowBackground` | `#EDEDED` | `#1C1C1E` | Main page surface |
| `CardBackground` | `#FFFFFF` | `#26262A` | Cards, panels, sidebar |
| `BorderColor` | `#E5E7EB` | `#333338` | Borders and separators |
| `TextPrimary` | `#000000` | `#FFFFFF` | Titles and primary content |
| `TextSecondary` | `#6B7280` | `#9CA3AF` | Descriptions and metadata |
| `PrimaryAccent` | `#671EE1` | `#671EE1` | Primary actions and selected states |
| `PrimaryHover` | `#7C3AED` | `#8B5CF6` | Hover and emphasis |
| `SidebarHoverTint` | `#15671EE1` | `#25671EE1` | Sidebar hover background |
| `PrimaryBackgroundBrush` | `#FFFFFF` | `#26262A` | Card aliases |
| `BorderBrush` | `#E5E7EB` | `#333338` | Component border alias |

### 5.2 Color rules

- Use `TextPrimary` for page titles, card values, and important labels.
- Use `TextSecondary` for helper text, descriptions, timestamps, and non-primary metadata.
- Use `PrimaryAccent` for one primary action per context.
- Use `PrimaryHover` only for hover or focused emphasis.
- Use green only for positive status or success information.
- Use amber/orange only for caution or edit affordances.
- Use red only for destructive actions or errors.
- Do not use color as the only indicator of status; pair it with text, an icon, or a state change.
- Add every theme-dependent resource to both light and dark dictionaries.

### 5.3 Contrast

Text must remain readable on both theme surfaces. In particular:

- Do not use `TextSecondary` for essential instructions.
- Do not place dark text directly on `PrimaryAccent` unless contrast has been checked.
- Verify placeholder text and disabled controls in both themes.
- Check Myanmar glyph readability separately from English text.

## 6. Typography

### 6.1 Hierarchy

Use a clear hierarchy:

| Level | Size | Weight | Example |
|---|---:|---|---|
| Page title | 24 px | Bold | Dashboard, resident management |
| Section title | 16 px | Bold | Occupancy by building |
| KPI value | 24–26 px | Bold | `84.5%`, `120` |
| Body/control text | 14 px | Regular or semi-bold | Button and form labels |
| Metadata | 12–13 px | Regular | Supporting descriptions |
| Small status | 11–12 px | Regular/light | Availability or state labels |

### 6.2 Font guidance

- Use `Segoe UI` for general English interface content, consistent with the dashboard view.
- Ensure the packaged `Pyidaungsu.ttf` remains available for PDF/document output and Myanmar content where required.
- Do not mix multiple decorative fonts.
- Prefer normal and semi-bold weights; reserve bold for titles and important values.
- Keep line lengths short in cards and helper text to avoid dense blocks.

### 6.3 Language and text length

Myanmar labels can occupy more horizontal space than English labels. Therefore:

- Prefer flexible grid columns over fixed widths for labels.
- Give filter labels and buttons enough room for the longest localized text.
- Avoid truncating resident, building, floor, or room names without a tooltip or detail view.
- Keep technical terms such as file extensions and backup formats in a predictable style.

## 7. Component Guidelines

### 7.1 Page headers

Every primary screen should begin with:

1. A 24 px bold title.
2. An optional 14 px supporting description below it.
3. A primary action aligned to the right when applicable.

Example structure:

```text
Page title                                      [Primary action]
Supporting description

Content
```

Use `TextPrimary` for the title and `TextSecondary` for the description.

### 7.2 Cards

Use cards for grouped information, KPIs, infrastructure items, settings, and summary sections.

Card rules:

- Background: `CardBackground`.
- Border: `BorderColor` with a 1 px border when separation is needed.
- Radius: 8–10 px.
- Padding: usually 16 px; use 20 px for large dashboard cards.
- Use the existing `DashboardCard` or `InteractiveCardStyle` where applicable.
- Do not put unrelated actions into the same card.
- Keep card content scannable with short labels and clear values.

### 7.3 Primary buttons

Use `PrimaryButtonStyle` for the main action in a page or form, such as:

- Add
- Save
- Export
- Confirm

Rules:

- Use one visually dominant primary action per context.
- Use action-oriented labels.
- Keep the button height consistent with nearby controls.
- Show a busy state or disable the button while saving.

### 7.4 Secondary buttons

Use `SecondaryButtonStyle` for:

- Cancel
- Back
- Clear
- Import when Export is the primary action
- Optional image actions

Secondary buttons should not compete visually with the page’s primary action.

### 7.5 Destructive actions

Delete actions should:

1. Use the existing destructive action style or a clear red visual treatment.
2. Include a delete icon where the surrounding component uses icons.
3. Ask for confirmation before changing data.
4. Explain what will be removed or deactivated.
5. Refuse the action when business rules prevent it, such as an occupied room.
6. Refresh affected lists and dashboard data after completion.

Never use a destructive action as the default focused button in a dialog.

### 7.6 Text inputs

Use `TextBoxStyle` for standard application text boxes.

Rules:

- Keep the label above the input for forms.
- Use placeholder text only for examples or search guidance; do not use it as the only label.
- Show validation near the relevant field.
- Preserve entered text after a validation failure.
- Use `UpdateSourceTrigger=PropertyChanged` for search and lightweight validation where appropriate.
- Keep input padding at approximately `12,10` and the corner radius at 6 px.

For WPF-UI text boxes, preserve the existing search pattern with an icon, clear button, and placeholder where it improves discoverability.

### 7.7 Combo boxes and cascading filters

Resident and student forms use cascading Building → Floor → Room selectors.

Rules:

- Disable or clear dependent selectors when the parent selection is cleared.
- Keep the selected value visible after data reload.
- Use `DisplayMemberPath="Name"` for infrastructure entities.
- Provide labels above each selector.
- Do not show stale floor or room choices after a building changes.

### 7.8 Data grids

Data grids are appropriate for residents, room members, and tabular academic records.

Rules:

- Keep the most identifying column first, normally name or record number.
- Keep columns focused on decision-making information.
- Use a dedicated action column for detail, edit, and delete commands.
- Prefer row actions over hidden context menus for frequent operations.
- Preserve adequate width for Myanmar names and NRC values.
- Provide an empty state when there are no records.
- Use pagination for large resident lists.

### 7.9 Images

Images should communicate identity or context, not decorate every surface.

Rules:

- Use packaged default images when no custom image exists.
- Keep previews inside a bounded container with consistent sizing.
- Preserve aspect ratio; do not stretch identity documents.
- Provide a larger preview for profile, NRC, and certificate images.
- Do not delete a custom image until the database update has succeeded.
- Protect default pack URI images from file deletion.

### 7.10 Icons

The project uses WPF-UI symbol icons and vector path data.

Rules:

- Use familiar, semantically accurate icons.
- Keep icons around 18–24 px in primary navigation and KPI cards.
- Pair icons with text for important actions.
- Do not use color alone to distinguish edit, delete, or status icons.
- Keep icon alignment consistent inside buttons and cards.

## 8. Screen-Specific Guidelines

### 8.1 Dashboard

The dashboard should answer “What is the current hostel situation?” quickly.

Recommended order:

1. Page title and short welcome/summary line.
2. Four KPI cards:
   - Active students
   - Occupancy rate
   - Total rooms
   - Available rooms
3. Building occupancy section.
4. Major distribution section.
5. Gender distribution section.
6. Academic year-level distribution section.

Dashboard rules:

- Use large values and short labels.
- Keep percentages paired with their denominator or capacity context.
- Use progress indicators only when the scale is clear.
- Show zero values intentionally rather than hiding empty categories.
- Refresh dashboard data after building, floor, room, or student changes.
- Keep charts and distribution sections readable at maximized-window sizes.

### 8.2 Building, floor, and room screens

These screens form a drill-down workflow and should feel like one consistent family.

Each screen should provide:

- A clear title or breadcrumb-like context.
- A visible Back action on nested pages.
- A primary Add action.
- An empty state with a direct Add action.
- Cards or rows with the entity name, image, status, and relevant count.
- Edit and delete actions in a predictable location.

Context should remain visible when navigating down the hierarchy. For example, the room screen should make the selected building and floor apparent.

### 8.3 Resident list

The resident page is a search and operations screen.

Recommended structure:

1. Page title and Add Resident action.
2. Search input.
3. Building, floor, and room filters.
4. Apply and reset actions.
5. Resident data grid.
6. Page-size selector and pagination.

Rules:

- Make search the most prominent control.
- Support Enter to apply search where the command is available.
- Keep filter labels visible above controls.
- Show active filters clearly.
- Preserve the current page size when the user moves between pages.
- Use a friendly empty state for no results and distinguish it from a loading state.

### 8.4 Student forms

Student forms contain many fields and should be divided into visual sections:

1. Room assignment
2. Personal information
3. NRC information
4. Parent information
5. Contact and address
6. Major and academic details
7. Images and documents
8. Academic history
9. Form actions

Rules:

- Do not present all fields as one undifferentiated block.
- Keep required-field indicators consistent.
- Put validation messages next to the field that needs correction.
- Use a stable two-column layout only where the window width supports it.
- Stack fields vertically at smaller widths.
- Keep Save and Cancel/Back actions together at the bottom.
- Disable Save while an operation is in progress.
- Warn before navigating away if unsaved form data protection is added later.

### 8.5 Room and student details

Detail screens should prioritize reading over editing.

- Use labeled sections and strong visual grouping.
- Show the primary identity information first.
- Present academic records in chronological or clearly labeled order.
- Make image previews discoverable but not intrusive.
- Keep Edit and Delete actions near the detail header or action group.
- Preserve the navigation context when returning to the room or resident list.

### 8.6 Data transfer

Import and export are high-consequence operations and require explicit language.

Export screen:

- Present category checkboxes with plain-language descriptions.
- Disable or reject Export when no category is selected.
- Show the selected destination or success state.

Import screen:

- Explain that existing records may be inserted or updated.
- Use a file picker restricted to `.hmsb` and `.json`.
- Show progress while processing.
- Show success or failure status after completion.
- Avoid allowing navigation or repeated import while the transaction is running.

### 8.7 Settings

The current settings screen contains a compact dark-mode card.

Guidelines for extending it:

- Keep settings grouped by topic in cards.
- Use a clear setting name and one-line explanation.
- Place toggles on the right side of the row.
- Make the entire row feel associated with the setting, while keeping the control keyboard-accessible.
- Persist settings consistently and reflect the current state immediately.
- Add new settings without changing the location or meaning of existing controls.

## 9. Interaction States

Every interactive component should define these states where relevant:

| State | Visual behavior |
|---|---|
| Default | Normal surface, border, and text |
| Hover | Subtle background, border, or accent change |
| Focused | Visible keyboard focus indication |
| Pressed | Shortened/elevated visual response |
| Disabled | Reduced emphasis but still readable |
| Loading | Spinner or busy overlay; prevent conflicting actions |
| Validation error | Local error message and clear field indication |
| Empty | Helpful explanation and next action |
| Success | Confirmation message or status update |
| Destructive confirmation | Explicit dialog before mutation |

Do not remove focus indicators to achieve a cleaner appearance. Keyboard users need a visible focus state.

## 10. Accessibility and Usability

### 10.1 Keyboard access

- All buttons, toggles, combo boxes, and inputs must be reachable by keyboard.
- Ensure tab order follows the visual order.
- Enter may submit or apply a search when that behavior is predictable.
- Escape should close transient previews or dialogs when supported.
- Do not make important actions available only through mouse hover.

### 10.2 Focus and feedback

- Use visible focus borders or WPF-UI focus states.
- Do not rely only on color changes for focus or validation.
- Keep feedback near the action that caused it.
- Use localized messages for database, import, export, and validation errors.

### 10.3 Readability

- Keep body text at approximately 14 px or larger for primary content.
- Avoid placing long text inside narrow cards.
- Preserve sufficient contrast in both themes.
- Allow labels to wrap or expand for Myanmar text instead of clipping.
- Use tooltips only as supplemental information, never as the only explanation for a critical action.

### 10.4 Data safety

- Confirm destructive actions.
- Do not silently discard form input.
- Show whether an operation is still running.
- Distinguish soft deletion from permanent deletion in user-facing language when relevant.
- Make import behavior clear before the user chooses a file.

## 11. Localization Guidelines

The current application uses English and Myanmar text together. New screens should support the same approach.

- Keep user-facing strings in a consistent language for a given screen where possible.
- Use Myanmar text for existing localized workflows unless the screen is intentionally English.
- Do not insert unnecessary English translations beside every Myanmar label; this increases visual density.
- Avoid hard-coding assumptions about label length.
- Test labels with the actual Pyidaungsu-supported glyphs.
- Check punctuation, parentheses, numerals, and NRC formatting in both languages.
- Reserve enough width for longer Myanmar action labels.

## 12. XAML Implementation Rules

### 12.1 Resource usage

Use dynamic resources for theme-dependent values:

```xml
<TextBlock Foreground="{DynamicResource TextPrimary}" />
<Border Background="{DynamicResource CardBackground}"
		BorderBrush="{DynamicResource BorderColor}" />
```

Use shared styles for common controls:

```xml
<Button Style="{StaticResource PrimaryButtonStyle}" />
<TextBox Style="{StaticResource TextBoxStyle}" />
```

Avoid duplicating the same template or color values in individual views.

### 12.2 Resource dictionaries

- Add shared dictionaries to `App.xaml` when they are application-wide.
- Use explicit same-assembly component URIs for local dictionaries.
- Keep theme dictionaries limited to semantic colors and theme-specific values.
- Keep component styles in component-specific dictionaries.
- Ensure XAML resource files use the correct WPF `Page` build action.
- Verify all referenced keys exist in both light and dark themes.

### 12.3 Binding conventions

- Prefer binding to view-model properties and generated commands.
- Use `RelativeSource` only when an item template needs its parent view-model command.
- Use `UpdateSourceTrigger=PropertyChanged` for live search or field validation when appropriate.
- Use `Mode=TwoWay` for editable selections and toggles.
- Provide `FallbackValue` or `TargetNullValue` for optional images and display fields.

### 12.4 Code-behind

Keep code-behind limited to view-specific concerns such as:

- Focus or visual behavior that cannot reasonably be expressed in XAML.
- Theme checkbox event integration already used by the settings view.
- View-only interactions that do not represent application state.

Business logic, persistence, validation, and navigation should remain in view models or services.

## 13. Recommended Reusable Components

As the UI grows, consider standardizing these reusable controls/styles:

- `PageHeader` with title, subtitle, and primary action slot
- `SectionCard` with theme-aware surface and border
- `EmptyState` with message and action
- `ValidationMessage` for consistent field errors
- `LoadingOverlay` for busy operations
- `ConfirmDeleteDialog` for destructive operations
- `ImagePreviewPanel` for profile and document images
- `FilterBar` for search and cascading filters
- `StatusBadge` for active, inactive, occupied, and available states

These should be introduced only when at least two screens need the same behavior. Until then, use the existing style dictionaries and keep duplication minimal.

## 14. Design Review Checklist

Before merging a UI change, verify:

### Layout

- [ ] The screen uses the standard content margin and spacing scale.
- [ ] The page title is visually clear and consistently placed.
- [ ] Flexible columns are used where Myanmar labels may expand.
- [ ] The screen scrolls correctly at smaller window sizes.
- [ ] No important control is hidden below an inaccessible region.

### Color and typography

- [ ] Theme-dependent colors use dynamic resource keys.
- [ ] Both light and dark themes have all required keys.
- [ ] Title, body, metadata, and status text follow the typography hierarchy.
- [ ] Contrast is acceptable in both themes.

### Interaction

- [ ] Primary and secondary actions are visually differentiated.
- [ ] Destructive actions require confirmation.
- [ ] Loading, disabled, empty, success, and error states are defined.
- [ ] Keyboard navigation and focus behavior are usable.
- [ ] Search and filters do not leave stale dependent selections.

### Data and localization

- [ ] User-facing text is localized consistently.
- [ ] Myanmar names and labels are not clipped.
- [ ] Image fallback behavior is defined.
- [ ] The UI explains import/export consequences.
- [ ] Database mutations refresh affected screens.

### Technical quality

- [ ] Existing shared styles are reused.
- [ ] No unnecessary business logic was added to code-behind.
- [ ] New view models have matching data templates.
- [ ] Resource dictionary URIs and Build Actions are correct.
- [ ] The project builds successfully after the change.

## 15. Design Decision Summary

The project should continue using:

- A 240 px persistent sidebar.
- A light-gray/dark-charcoal surface system.
- Purple accent actions with semantic theme resources.
- 8–10 px card radii and 16–20 px card padding.
- 24 px bold page titles and 14 px supporting text.
- Cards for summaries and grouped settings.
- Data grids for resident and room-member operations.
- Explicit confirmation for deletion.
- Loading overlays for database and file operations.
- Dynamic resources for all theme-aware visual values.
- MVVM commands and messenger-driven navigation.

This guideline should be updated when a new global component, theme token, navigation pattern, or localization rule is introduced.
