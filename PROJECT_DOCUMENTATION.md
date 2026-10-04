# Hostel Management System

## 1. Project Overview

**Hostel Management System (HMS)** is a Windows desktop application for managing hostel infrastructure and residents. It is implemented as a WPF application using the MVVM pattern, Entity Framework Core with SQLite for local persistence, CommunityToolkit.Mvvm for observable state and commands, WPF-UI for selected controls and appearance support, and QuestPDF for document/PDF-related student functionality.

The application manages the following hierarchy:

```text
Building
└── Floors
	└── Rooms
		└── Students
			└── Academic records
```

The user interface includes:

- Dashboard statistics and distributions
- Building, floor, and room management
- Resident/student registration and filtering
- Student details, academic history, image previews, editing, and deletion
- JSON-based backup and restore through `.hmsb` or `.json` files
- Light/dark theme support through persisted application settings
- Myanmar-language labels and NRC input support

The project is a single executable desktop application. It does not expose a web API and does not require a separate database server.

## 2. Technology Stack

| Area | Technology |
|---|---|
| Application type | WPF desktop application |
| Language | C# |
| Target framework | `net10.0-windows` |
| Project SDK | `Microsoft.NET.Sdk` |
| UI framework | Windows Presentation Foundation (WPF) |
| MVVM | CommunityToolkit.Mvvm `8.4.2` |
| Persistence | Entity Framework Core SQLite `10.0.11` |
| EF tooling | Microsoft.EntityFrameworkCore.Tools `10.0.11` |
| UI controls/theme | WPF-UI `4.3.0` |
| PDF/document support | QuestPDF `2026.9.1` |
| Serialization | `System.Text.Json` |
| Nullable reference types | Enabled |
| Implicit usings | Enabled |

The application currently targets .NET 10 and uses WPF-specific Windows APIs, so it is intended to run on Windows.

## 3. Repository Structure

```text
HostelManagementSystem/
├── App.xaml                         Application resources and startup URI
├── App.xaml.cs                      Startup initialization
├── MainWindow.xaml                  Main shell, sidebar, content host, loading overlay
├── MainWindow.xaml.cs               Main window code-behind
├── HostelManagementSystem.csproj    Project configuration and package references
│
├── Database/
│   └── AppDbContext.cs              SQLite configuration and EF relationships
├── Models/                          EF Core entities and domain helpers
├── DTOs/                            Data-transfer and grid projection types
├── Services/                        Operations that span persistence and file cleanup
├── Messages/                        CommunityToolkit messenger message types
├── Utilities/                       Theme, image, settings, loading, and NRC helpers
├── ViewModels/                      Observable state and application commands
├── Views/                           WPF UserControls
├── Resources/                       Data templates for ViewModel-to-View mapping
├── Styles/                          Shared controls, themes, and component styles
├── Image/                           Packaged default and application images
├── Assets/Fonts/                    Packaged Pyidaungsu font
└── favicon.ico                      Application icon
```

The `Helpers` namespace is used by some utility classes even though the physical source files are under `Utilities`.

## 4. Application Startup

`App.xaml` defines `MainWindow.xaml` as the startup window and merges the following resources:

1. WPF-UI theme and control dictionaries
2. ViewModel-to-View data templates
3. Sidebar styles
4. Button styles
5. Light theme resources
6. TextBox styles
7. Interactive card styles

Local dictionaries use explicit same-assembly component URIs such as:

```text
/HostelManagementSystem;component/Styles/TextBoxStyles.xaml
```

`App.xaml.cs` performs the following startup tasks:

1. Applies the saved light/dark theme from `settings.json`.
2. Registers `Assets/Fonts/Pyidaungsu.ttf` with QuestPDF if the font file exists.
3. Applies the WPF-UI accent color `#4F46E5` and dark application accent configuration.
4. Creates the SQLite database and schema using `EnsureCreated()`.

Database creation happens before normal user interaction. There are no EF Core migrations in the repository; schema evolution is therefore currently based on the `EnsureCreated()` approach rather than migration scripts.

## 5. Architecture

The application follows a practical MVVM structure:

```text
MainWindow
   │
   ├── MainViewModel
   │      ├── DashboardViewModel
   │      ├── BuildingViewModel
   │      ├── ResidentViewModel
   │      ├── SettingViewModel
   │      └── DataTransferViewModel
   │
   ├── CurrentView binding
   │      └── DataTemplates.xaml resolves ViewModel to UserControl
   │
   ├── AppDbContext ── SQLite database
   ├── LoadingHelper ── BusyMessage ── loading overlay
   └── WeakReferenceMessenger ── navigation and database refresh messages
```

### 5.1 MVVM implementation

View models inherit from `ObservableObject`, `ObservableRecipient`, or `ObservableValidator` from CommunityToolkit.Mvvm. Attributes such as `[ObservableProperty]` and `[RelayCommand]` generate properties and `ICommand` implementations at compile time.

Most views are `UserControl` instances. The main window binds its `ContentControl` to `MainViewModel.CurrentView`. The matching `DataTemplate` in `Resources/DataTemplates.xaml` creates the appropriate view automatically based on the current view model type.

### 5.2 Navigation

Sidebar buttons invoke `MainViewModel.NavigateToCommand` with one of:

- `Dashboard`
- `Resident`
- `Building`
- `DataTransfer`
- `Setting`

Nested pages do not use a separate frame. They create a new view model and send a `NavigateMessage` through `WeakReferenceMessenger`. `MainViewModel` receives the message and replaces `CurrentView`.

Typical hierarchy navigation is:

```text
BuildingView
  → FloorView
	→ RoomView
	  → RoomDetailView
		→ StudentDetailView
```

Create and edit forms use the same message-based navigation approach and return to their parent view after saving or cancelling.

### 5.3 Busy/loading state

`LoadingHelper.ExecuteWithLoadingAsync` sends a `BusyMessage(true, text)` before an asynchronous operation and always sends `BusyMessage(false)` in a `finally` block. `MainViewModel` receives the message and exposes `IsBusy` and `BusyText`. The main window uses those properties to display the loading overlay.

## 6. Persistence and Database

### 6.1 Database location

`AppDbContext` stores the database at:

```text
%AppData%\HostelManagementSystem\hostel.db
```

The directory is created automatically if it does not exist. The database uses SQLite and is accessed through a new `AppDbContext` instance for each operation.

Application settings are stored separately at:

```text
%AppData%\HostelManagementSystem\settings.json
```

User-selected image files are stored under the local application data directory:

```text
%LocalAppData%\HostelManagementSystem\
├── BuildingImages\
├── FloorImages\
├── RoomImages\
└── student-related image folders as used by form workflows
```

### 6.2 Entity relationships

| Relationship | Behavior |
|---|---|
| Building → Floor | One-to-many; database cascade delete configured |
| Floor → Room | One-to-many; database cascade delete configured |
| Room → Student | One-to-many; nullable student room; deleting a room sets `RoomId` to null |
| Student → StudentAcademicRecord | One-to-many; deleting a student cascades to academic records |

The application generally uses soft deletion for buildings, floors, rooms, and students by setting `IsActive = false`. Queries for active management screens usually filter on `IsActive`.

### 6.3 Indexes

The context defines indexes for common active-record and hierarchy queries:

- `Building.IsActive`
- `(Floor.BuildingId, Floor.IsActive)`
- `(Room.FloorId, Room.IsActive)`
- `Student.IsActive`
- `(Student.RoomId, Student.IsActive)`
- `StudentAcademicRecord.StudentId`

## 7. Domain Model

### 7.1 Building

`Models/Building.cs` contains:

- `Id`
- Required `Name`
- `Img`
- `IsActive`
- `Floors`

Buildings are loaded as active records in the building management screen. Deleting a building is implemented as a cascade soft delete for its floors and rooms, and custom images are removed from local storage after the database update succeeds.

### 7.2 Floor

`Models/Floor.cs` contains:

- `Id`
- `BuildingId`
- Required `Name`
- `Img`
- `IsActive`
- `Building`
- `Rooms`

Floors are displayed in the context of a selected building. Deleting a floor soft-deletes the floor and all rooms below it.

### 7.3 Room

`Models/Room.cs` contains:

- `Id`
- `FloorId`
- Required `Name`
- `Img`
- `Capacity`
- `IsActive`
- `Floor`
- `Students`

Computed UI helpers:

- `CurrentResidentsCount`: counts active students in the loaded student collection.
- `IsAvailableForBooking`: checks room, floor, and building active status.

A room cannot be deleted when its loaded active resident count is greater than zero.

### 7.4 Student

`Models/Student.cs` contains required personal, contact, parent, and academic fields:

- `Id`
- Nullable `RoomId`
- `Name`
- `Img`
- `Birthday`
- `NRC`
- `Student_ph`
- `Parent_ph`
- `Address`
- `Gender`
- `Major`
- `Father_name`
- `Mother_name`

Optional fields include:

- Front and back NRC images
- Father and mother NRC values
- High-school certificate image
- High-school roll number
- High-school name
- High-school city

Metadata and relationships:

- `AcademicRecords`
- `IsActive`
- `CreatedAt`
- `UpdatedAt`
- `LatestRollNumber`, a computed property based on the most recently inserted academic record

### 7.5 Student academic record

`Models/StudentAcademicRecord.cs` stores historical academic information independently from the main student row:

- `StudentId`
- `AcademicYearLevel`
- `AttemptNumber`
- `RollNumber`
- `AttendedYear`
- `IsHonours`
- `CreatedAt`

The one-to-many design allows a student to retain multiple academic years, attempts, and honours records.

### 7.6 NRC township data

`NrcTownship` represents Myanmar National Registration Card location data. It provides English and Myanmar formatted prefixes, including Myanmar digit conversion. `NrcManager` is a singleton data provider containing the township table and supports region, state, township, code, citizen-type, and full-NRC validation operations.

## 8. Main Features

### 8.1 Dashboard

`DashboardViewModel` calculates and exposes:

- Total active students
- Total active rooms
- Available rooms based on capacity
- Total room capacity
- Occupancy percentage
- Building-level occupancy
- Major distribution
- Gender distribution
- Academic year-level distribution based on each active student’s latest academic record

The dashboard reloads when it receives a `DatabaseUpdatedMessage` for a building, floor, room, or student. The dashboard view presents KPI cards and distribution sections using `Styles/DashboardStyles.xaml`.

### 8.2 Building management

`BuildingViewModel` supports:

- Loading active buildings
- Empty-state handling
- Opening the create form
- Opening the edit form
- Selecting a building to view floors
- Confirming and soft-deleting a building
- Deleting custom building images after successful persistence

Building forms use `BuildingFormBaseViewModel` for shared validation, image selection, preview handling, and navigation behavior. The create and edit implementations are:

- `CreateBuildingFormViewModel`
- `EditBuildingFormViewModel`

### 8.3 Floor management

`FloorViewModel` is created for a selected building and supports:

- Loading active floors belonging to that building
- Opening create and edit forms
- Selecting a floor to view rooms
- Returning to the building list
- Confirming and soft-deleting a floor and its rooms

Shared form behavior is in `FloorFormBaseViewModel`. Concrete forms are:

- `CreateFloorFormViewModel`
- `EditFloorFormViewModel`

### 8.4 Room management

`RoomViewModel` is created for a selected building and floor and supports:

- Loading active rooms for the selected floor
- Loading students with rooms to calculate resident counts
- Opening create and edit forms
- Opening room details
- Returning to the floor list
- Preventing deletion of occupied rooms
- Soft-deleting empty rooms
- Notifying the dashboard after a room is deleted

Shared room form behavior is in `RoomFormBaseViewModel`. It validates room names and capacity, allowing a capacity from 1 through 1000, and supports image selection and preview.

### 8.5 Resident management

`ResidentViewModel` provides the main student list with:

- Name search
- Building, floor, and room cascading filters
- Page-size choices of 10, 20, and 50
- Pagination
- Reset and apply-filter commands
- Add-student navigation
- Student detail navigation
- Student edit navigation
- Student deletion through `StudentService`

The cascading filter sequence is:

```text
Selected building → available floors
Selected floor    → available rooms
Selected room     → resident query filter
```

### 8.6 Student registration and editing

`AddNewStudentFormViewModel` and `EditStudentFormViewModel` support:

- Personal information
- Parent information
- Gender selection
- Birthday input
- NRC input for the student and parents
- Major selection
- Room assignment through cascading building/floor/room selectors
- Student profile image
- NRC front and back images
- High-school certificate image
- High-school information
- Multiple academic records
- Save, cancel, and return navigation

The forms use sub-view models for focused input behavior:

- `NrcInputViewModel`
- `BirthdayInputViewModel`
- `MajorInputViewModel`
- `AcademicRecordInputViewModel`

Student image files are copied into application data storage by `ImageManager`. Existing files and packaged default images are protected from accidental deletion where the image manager recognizes them as defaults.

### 8.7 Room details

`RoomDetailViewModel` projects active students assigned to the selected room into `StudentDataGridDto`. The projection fetches only the columns needed by the room detail grid and includes the latest roll number.

Room detail actions include:

- Open student detail
- Edit student
- Delete student
- Add a new student in the room context
- Return to the room list

### 8.8 Student details

`StudentDetailViewModel` loads a student with academic records and provides:

- Profile and contact details
- Academic history
- Profile, NRC, and certificate image paths
- Image preview open/close behavior
- Edit-student navigation
- Back navigation
- PDF/document-related actions implemented with QuestPDF support

The view model resolves both pack URIs and existing local file paths before displaying images.

### 8.9 Data transfer

`DataTransferViewModel` exports and imports a JSON payload using the `.hmsb` or `.json` file extensions.

Export options:

- Students and academic records
- Infrastructure: buildings, floors, and rooms
- Both categories

Export behavior:

1. The user chooses the categories.
2. A save dialog selects the destination.
3. The database is queried with `AsNoTracking()`.
4. The payload is serialized directly to a file stream.
5. Circular references are ignored by `System.Text.Json`.

Import behavior:

1. The user selects a backup file.
2. The payload is deserialized from a file stream.
3. EF change detection is temporarily disabled for the batch.
4. The operation runs inside a database transaction.
5. Entities are inserted or updated by ID using batch upsert logic.
6. Updated entity types are announced through database messages.
7. The transaction is committed or rolled back on failure.

### 8.10 Settings and themes

`SettingsManager` stores a small JSON configuration containing `IsDarkMode`. It safely falls back to default settings when the file is missing or invalid.

`ThemeManager`:

- Loads the saved configuration at startup
- Swaps the light or dark resource dictionary at runtime
- Persists the selected mode asynchronously

Theme resources define common keys including:

- `WindowBackground`
- `CardBackground`
- `BorderColor`
- `TextPrimary`
- `TextSecondary`
- `PrimaryAccent`
- `PrimaryHover`
- `SidebarHoverTint`
- `PrimaryBackgroundBrush`
- `BorderBrush`

The current `SettingViewModel` is intentionally minimal; the application has theme infrastructure, but the settings screen itself contains little view-model behavior at present.

## 9. UI Resources and Styling

### 9.1 Data templates

`Resources/DataTemplates.xaml` maps view model types to views. It covers dashboard, building, floor, room, room detail, student detail, student forms, infrastructure forms, resident, settings, and data-transfer views.

This allows the main `ContentControl` to display a view without manually constructing the view in navigation code.

### 9.2 Shared styles

| File | Responsibility |
|---|---|
| `Styles/SidebarStyles.xaml` | Sidebar radio-button template and active/hover states |
| `Styles/ButtonStyles.xaml` | Primary, secondary, and WPF-UI action button styles |
| `Styles/TextBoxStyles.xaml` | Rounded TextBox template and focus/hover states |
| `Styles/InteractiveCardStyle.xaml` | Card border, shadows, and hover highlighting |
| `Styles/DashboardStyles.xaml` | Dashboard cards, KPI labels, and section headers |
| `Styles/Themes/LightTheme.xaml` | Light color resources |
| `Styles/Themes/DarkTheme.xaml` | Dark color resources |

### 9.3 Assets

Packaged resources include:

- `Image/hms_logo.png`
- `Image/tht_logo.png`
- Default building, floor, room, and student images
- `Assets/Fonts/Pyidaungsu.ttf`
- `favicon.ico`

Default images are referenced through WPF pack URIs such as:

```text
pack://application:,,,/Image/DefaultImages/default_room.png
```

## 10. Messages and Cross-Component Updates

The application uses CommunityToolkit's weak-reference messenger to reduce direct coupling between view models.

### `NavigateMessage`

Carries a target `ObservableObject` view model to `MainViewModel`, which replaces the current content.

### `BusyMessage`

Carries the loading state and optional localized message used by the main loading overlay.

### `DatabaseUpdatedMessage`

Carries:

- Entity name
- Entity ID
- Operation (`Inserted`, `Updated`, or `Deleted`)
- UTC timestamp

The dashboard listens for relevant entity updates and reloads its metrics.

## 11. Image Management

`ImageManager` is responsible for user-selected local images.

### Save behavior

- Empty source paths reuse an existing path or return a default.
- Unchanged paths are reused.
- Missing files and default image references are not copied.
- New images are copied to an application-data subfolder with a generated unique filename.
- Copy failures are logged to the debug output and fall back to the default image.

### Delete behavior

- Empty and recognized default paths are ignored.
- Existing custom files are deleted.
- deletion failures are written to the debug output rather than crashing the UI.

## 12. Build and Run Instructions

### Prerequisites

- Windows
- Visual Studio with the .NET 10 SDK and WPF workload installed, or an equivalent .NET 10 SDK installation
- Access to restore NuGet packages

### Visual Studio

1. Open `HostelManagementSystem.slnx`.
2. Select the `HostelManagementSystem` project as the startup project.
3. Restore NuGet packages if Visual Studio does not restore automatically.
4. Build the solution.
5. Run with debugging or without debugging.

### Command line

From the repository root:

```powershell
dotnet restore
dotnet build HostelManagementSystem.csproj
dotnet run --project HostelManagementSystem.csproj
```

The project is configured as a Windows executable (`WinExe`).

### Output

The project currently specifies an absolute `BaseOutputPath` in the project file. This path is machine-specific:

```text
C:\Users\Asus\Desktop\C# dotnet\WPF\HostelManagementSystem\bin
```

When using another computer or checkout location, this setting should be reviewed and preferably changed to a repository-relative or default output path.

## 13. Data Lifecycle Examples

### Creating a room

```text
User selects building
→ selects floor
→ opens Create Room
→ enters name and capacity
→ optionally selects an image
→ form validates input
→ ImageManager stores the image
→ Room is inserted into SQLite
→ room list/dashboard can refresh
```

### Deleting a student

```text
User opens a room
→ selects a student
→ confirms deletion
→ StudentService loads student and academic records
→ custom images are removed
→ academic records are removed
→ student is removed from SQLite
→ room detail list is refreshed
```

### Importing a backup

```text
User selects infrastructure/student options
→ selects a backup file
→ JSON is deserialized
→ EF change tracking is optimized for the batch
→ records are upserted in a transaction
→ transaction commits
→ database-update notifications refresh dependent screens
```

## 14. Validation and Error Handling

Validation is distributed according to the feature:

- Building and floor names use `ObservableValidator`, required validation, and minimum length rules.
- Room names use required and minimum length validation.
- Room capacity uses numeric validation and a 1–1000 range check.
- NRC fields use `NrcManager` data and validation methods.
- Delete operations use confirmation dialogs.
- Database operations commonly catch `DbUpdateException` or general exceptions and show a localized message.
- Settings and image file operations fall back safely and write diagnostics to the Debug output.
- `LoadingHelper` ensures the busy state is cleared even when an operation throws.

## 15. Testing and Verification

No dedicated test project was found in the current solution structure. The primary verification workflow is therefore:

1. Build the project.
2. Launch the application.
3. Verify resource dictionaries load without XAML exceptions.
4. Verify database creation in `%AppData%`.
5. Exercise dashboard, infrastructure CRUD, resident filters, image selection, student CRUD, and data export/import.
6. Inspect the Visual Studio Output window for debug diagnostics.

A future test project would be particularly useful for:

- NRC validation
- Image-manager default-image protection
- Capacity and occupancy calculations
- Import upsert and transaction behavior
- Student academic-record mapping
- Theme settings load/save behavior

## 16. Current Implementation Considerations

The following items are important when maintaining or deploying the project:

1. **Database schema management**: the application uses `EnsureCreated()` and does not contain migration files. A migration strategy should be introduced before distributing schema-changing releases.
2. **Machine-specific output path**: `BaseOutputPath` is currently an absolute path tied to one workstation.
3. **Local file dependencies**: custom images and settings are stored in user profile directories and are not automatically portable with the executable.
4. **Default image path consistency**: some model default strings use an `Images` path while packaged resources use `Image`. New code should consistently use the actual packaged `Image` directory.
5. **Soft deletion**: active screens hide inactive records, but the database retains soft-deleted rows. Backup/import behavior should preserve this policy intentionally.
6. **Settings screen maturity**: theme infrastructure exists, but `SettingViewModel` currently has minimal behavior.
7. **Runtime XAML diagnostics**: if a merged resource fails, inspect the inner exception and verify the resource URI, Build Action, and referenced WPF-UI resources.
8. **Startup database work**: `EnsureCreated()` runs during application startup. Large future migrations or seed operations should be moved to a controlled initialization path.
9. **No automated test suite**: changes to persistence, resource dictionaries, and generated MVVM commands should be manually verified until automated tests are added.

## 17. Maintenance Guidelines

When adding a new screen:

1. Add the view model under the appropriate `ViewModels` folder.
2. Add the view under the appropriate `Views` folder.
3. Add a `DataTemplate` entry in `Resources/DataTemplates.xaml`.
4. Add a navigation property or command in `MainViewModel` if it is a top-level screen.
5. Use `NavigateMessage` for nested navigation.
6. Use `LoadingHelper` for asynchronous database/file operations.
7. Send `DatabaseUpdatedMessage` after mutations that affect dashboard or dependent screens.
8. Keep resource dictionary URIs explicit and ensure the XAML Build Action is `Page`.

When changing the data model:

1. Update the entity and navigation properties.
2. Update `AppDbContext` relationship and index configuration.
3. Update DTOs and import/export payloads.
4. Update relevant view models and XAML bindings.
5. Decide whether the change requires an EF migration strategy.
6. Test both a new database and an existing user database.

When changing images or themes:

1. Update the `.csproj` resource item if the file is packaged.
2. Use the correct `Image` versus `Images` directory.
3. Protect packaged defaults from `ImageManager.DeleteImage`.
4. Add matching keys to both light and dark dictionaries.
5. Verify resource load order in `App.xaml`.

## 18. Summary

Hostel Management System is a local-first WPF application organized around a building → floor → room → student hierarchy. Its principal strengths are a clear MVVM navigation model, SQLite persistence without external infrastructure, localized UI support, academic-history tracking, image lifecycle handling, dashboard aggregation, and backup/import capabilities.

The most important operational files are:

- `App.xaml` and `App.xaml.cs` for startup and resources
- `MainWindow.xaml` and `ViewModels/MainViewModel.cs` for the application shell
- `Database/AppDbContext.cs` for storage configuration
- `Models/` for the domain schema
- `ViewModels/` and `Views/` for user workflows
- `Resources/DataTemplates.xaml` for view resolution
- `Utilities/` for themes, settings, image handling, loading, and NRC logic
