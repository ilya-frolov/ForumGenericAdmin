# Dino Generic Admin - Complete Framework Documentation

**Reference Location:** This documentation is referenced by `.cursorrules` in the project root.

For Cursor AI rules and constraints, see: `../../.cursorrules`

---

# Cursor Rules Configuration
# Dino Generic Admin - .NET Core Admin Panel Framework
# NOTE: This is a template project. Replace "DinoGenericAdmin" with your project name (e.g., "ProjectName")

## Project Overview

This is a generic administration panel framework built on .NET Core 9+. The admin panel automatically generates CRUD interfaces (list pages, create/edit pages) based on model definitions and controllers, requiring NO client-side code. The system uses smart attributes to configure field types, visual containers, mappings, and complex input types.

### Key Features
- **Zero Client-Side Code Required**: Admin interfaces are auto-generated from C# models and controllers
- **Smart Attributes System**: Extensive attribute system for field configuration, visibility, containers, tabs
- **Integrated Systems**: Pre-built integrations for cache (Redis - optional), Hangfire, Azure Blob Storage, EF Core helpers
- **Role-Based Access**: Built-in admin user and role management with DinoAdmin master role
- **OTP Authentication**: Integrated login and OTP systems
- **Smart Search & Filtering**: Advanced filtering with multiple operators and conditions
- **Platform-Specific Uploads**: File uploads can be separated by platform (Desktop, Mobile, Tablet, App, Custom1-3)

## Architecture

### Technology Stack
- **Backend**: .NET Core 9+
- **Database**: Microsoft SQL Server (MSSQL) - Always MSSQL, all DB scripts must be for MSSQL
- **Frontend**: Auto-generated (no client-side code required)
- **ORM**: Entity Framework Core
- **Caching**: In-memory (Redis optional, usually not used)
- **Background Jobs**: Hangfire (optional)
- **Storage**: File system or Azure Blob Storage (optional)

### Core Submodules
- **Dino.CoreMvc.Admin**: Main MVC admin framework (controllers, models, attributes, field type plugins)
  - Located in: `Submodules/Dino.CoreMvc.Admin/`
  - Key locations:
    - Admin Models Base: `Models/Admin/BaseAdminModel.cs`
    - Field Type Attributes: `Attributes/FieldTypes.cs`
    - Admin Properties Attributes: `Attributes/ColumnTypes.cs`, `Attributes/General.cs`
    - Controllers Base: `Controllers/DinoAdminBaseEntityController.cs`
- **Dino.Core.AdminBL**: Business logic layer (BaseBL, BLFactory, cache management, data access)
  - Located in: `Submodules/Dino.Core.AdminBL/`
- **Dino.Common.Hangfire**: Hangfire integration extensions
- **Dino.Common.AzureExtensions**: Azure Blob Storage integration
- **Dino.Common.EfCoreHelpers**: EF Core pagination and query helpers

### Project Structure
**IMPORTANT**: Replace `DinoGenericAdmin` with your project name (e.g., `ProjectName`)

```
ProjectName/
├── ProjectName.Api/                    # ASP.NET Core API project
│   ├── Areas/Admin/                    # Admin area controllers and models
│   │   ├── Controllers/                # Admin entity controllers (NO subfolders except Demo/Basics)
│   │   └── Models/                    # Admin model classes (NO subfolders except Demo/Basics)
│   │       ├── Basics/                 # Required: AdminUser, AdminRole models
│   │       └── Demo/                   # DELETE THIS when starting new project
│   ├── ModelsSettings/                # Settings models (SystemSettings, DinoMasterSettings)
│   └── Program.cs                     # Application startup and configuration
├── ProjectName.BL/                     # Business logic layer
│   ├── Cache/                         # Cache manager implementation
│   ├── Contracts/                     # BL interfaces
│   ├── Data/                          # DbContext and entities (DB models)
│   └── Models/                        # Cache models and other BL models (NO subfolders except Demo/Basics)
│       ├── Basics/                     # Required: AdminUser, AdminRole entities
│       └── Demo/                      # DELETE THIS when starting new project
└── Submodules/                        # Git submodules (DO NOT MODIFY)
    ├── Dino.CoreMvc.Admin/            # Core admin framework
    ├── Dino.Core.AdminBL/             # Core BL framework
    └── Dino.Common.*/                 # Common utilities
```

**Path Structure Notes:**
- Admin Models: `ProjectName.Api/Areas/Admin/Models/` (root level, no subfolders except Demo/Basics)
- Admin Controllers: `ProjectName.Api/Areas/Admin/Controllers/` (root level, no subfolders except Demo/Basics)
- BL Models: `ProjectName.BL/Models/` (root level, no subfolders except Demo/Basics)
- **Cache Models: `ProjectName.BL/Cache/Models/`** (create this folder structure)
- **Demo folders (`Demo/`) are for examples only - DELETE when starting a new project**
- **Basics folders (`Basics/`) contain required admin user/role models - KEEP these**

**Project Structure Cleanup (CRITICAL):**
When starting a new project, ensure you work in the correct project folders:
- **DO NOT** create files in AdminClient project (`DinoGenericAdmin/` folder)
- **ALWAYS** create BL models in `DinoGenericAdmin.BL/Models/`
- **ALWAYS** create Cache models in `DinoGenericAdmin.BL/Cache/Models/`
- **ALWAYS** create API models in `DinoGenericAdmin.Api/Areas/Admin/Models/`
- **ALWAYS** create controllers in `DinoGenericAdmin.Api/Areas/Admin/Controllers/`

If you accidentally create files in the wrong location, move them to the correct project folders.

## Backend Patterns & Conventions

### Entity, Model, and Controller Relationship

**CRITICAL**: Each entity requires THREE components with matching naming:

1. **Entity Class** (DB Model): `ProjectName.BL/Data/` or `ProjectName.BL/Models/`
   - Pure database entity (EF Core)
   - Example: `Item.cs` (class `Item`)

2. **Admin Model**: `ProjectName.Api/Areas/Admin/Models/`
   - Inherits `BaseAdminModel`
   - Naming: `Admin{EntityName}Model`
   - Example: `AdminItemModel.cs` (class `AdminItemModel`)

3. **Admin Controller**: `ProjectName.Api/Areas/Admin/Controllers/`
   - Inherits `DinoAdminBaseEntityController<TModel, TEntity, TId>`
   - Naming: `Admin{EntityName}Controller` (plural for collections)
   - Example: `AdminItemsController.cs` (class `AdminItemsController`)

**Naming Convention is CRITICAL:**
- Entity: `Item` → Admin Model: `AdminItemModel` → Controller: `AdminItemsController`
- Entity: `Category` → Admin Model: `AdminCategoryModel` → Controller: `AdminCategoriesController`
- The system relies on these naming patterns for auto-discovery and mapping

### Base Controller Generic Parameters

The base controller uses three generic parameters:
```csharp
DinoAdminBaseEntityController<TModel, TEntity, TId>
```

- **TModel**: The Admin Model type (e.g., `AdminItemModel`)
- **TEntity**: The Database Entity type (e.g., `Item`)
- **TId**: The type of the ID property (e.g., `int`, `string`, `Guid`)

**CRITICAL**: The ID property type MUST match between Admin Model and Entity:
- If Entity has `public int Id { get; set; }`
- Then Admin Model MUST have `public int Id { get; set; }`
- And Controller uses `DinoAdminBaseEntityController<AdminItemModel, Item, int>`

### Admin Models (BaseAdminModel)

All admin models MUST inherit from `BaseAdminModel`:

```csharp
public class AdminItemModel : BaseAdminModel
{
    [AdminFieldCommon("Name", required: true, tooltip: "Item name")]
    [AdminFieldText(maxLength: 100)]
    [ListSettings]
    public string Name { get; set; }
}
```

**Key Points:**
- Every property MUST have `[AdminFieldCommon]` attribute (defines label, tooltip, required, readonly, visibility)
- Every property MUST have a field type attribute (`[AdminFieldText]`, `[AdminFieldNumber]`, etc.)
- Use `[ListSettings]` to include property in list view
- Use `[VisibilitySettings]` to control when fields appear (create vs edit vs view)
- Use `[SortIndex]` for properties that control ordering
- Use `[SaveDate]`, `[LastUpdateDate]`, `[UpdatedBy]` for audit fields
- Use `[ArchiveIndicator]`, `[DeletionIndicator]` for soft delete/archive fields

**Where to Find Admin Properties:**
- `Submodules/Dino.CoreMvc.Admin/Attributes/ColumnTypes.cs` - Special property markers (SaveDate, SortIndex, etc.)
- `Submodules/Dino.CoreMvc.Admin/Attributes/General.cs` - ListSettings, VisibilitySettings, etc.

**Property Visibility:**
Use `[VisibilitySettings]` to control when fields appear in different form modes, with the parameters:
- `showOnCreate` - Show field during entity creation (default: true)
- `showOnEdit` - Show field during entity editing (default: true)
- `showOnView` - Show field in view/list modes (default: true)
- Shortcut: Use `[VisibilitySettings(false)]` to hide field in all modes

### Field Type Attributes

**Where to Find Field Types:**
- All field type attributes are in: `Submodules/Dino.CoreMvc.Admin/Attributes/FieldTypes.cs`

**Text Fields:**
- `[AdminFieldText]` - Single-line text input
- `[AdminFieldTextArea]` - Multi-line text area
- `[AdminFieldRichText]` - Rich text editor (Quill/CKEditor)
- `[AdminFieldPassword]` - Password input with strength validation
- `[AdminFieldURL]` - URL input with validation

**Numeric Fields:**
- `[AdminFieldNumber]` - Number input (integer or decimal)

**Selection Fields:**
- `[AdminFieldSelect]` - Dropdown/select (supports DbType, Enum, Function sources)
- `[AdminFieldMultiSelect]` - Multi-select (can store as JSON or EF relationship)

#### MultiSelect Storage Scenarios

MultiSelect supports three storage scenarios:

**Scenario 1: JSON Storage (simplest)**
Store selected IDs as a JSON array in a single column. Best for simple cases without complex querying needs.

```csharp
// Admin Model
[AdminFieldCommon("Categories")]
[AdminFieldMultiSelect(SelectSourceType.DbType, typeof(Categories), "Name", "Id", storeAsJson: true)]
public List<int>? CategoryIds { get; set; }

// Entity - just a string column
public string CategoryIds { get; set; }  // Stores as "[1,2,3]"
```

```sql
-- Database: Simple string column
CategoryIds NVARCHAR(MAX) NULL
```

**Scenario 2: Junction Table WITH ID Column**
Use when you have a junction table entity with its own ID (allows additional properties on the relationship).

```csharp
// Junction Entity (has its own Id)
public class BreakerTypeBreakerActionTypes
{
    public int Id { get; set; }
    public int BreakerTypeId { get; set; }
    public int BreakerActionTypeId { get; set; }
    // Can have additional properties like SortIndex, CreateDate, etc.
    public virtual BreakerTypes BreakerType { get; set; }
    public virtual BreakerActionTypes BreakerActionType { get; set; }
}

// Parent Entity
public class BreakerTypes
{
    public int Id { get; set; }
    public string Name { get; set; }
    public virtual ICollection<BreakerTypeBreakerActionTypes> BreakerTypeBreakerActionTypes { get; set; }
}

// Admin Model - property name MUST match the navigation property name
[AdminFieldCommon("Available Action Types")]
[AdminFieldMultiSelect(SelectSourceType.DbType, typeof(BreakerActionTypes), "Name", "Id", 
    relatedEntityIdProperty: nameof(BreakerTypeBreakerActionTypes.BreakerActionTypeId), 
    storeAsJson: false)]
public List<int>? BreakerTypeBreakerActionTypes { get; set; }
```

```sql
-- Database: Junction table with ID
CREATE TABLE BreakerTypeBreakerActionTypes
(
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    BreakerTypeId       INT NOT NULL,
    BreakerActionTypeId INT NOT NULL,
    -- Additional columns as needed
    CONSTRAINT FK_... FOREIGN KEY(BreakerTypeId) REFERENCES BreakerTypes(Id),
    CONSTRAINT FK_... FOREIGN KEY(BreakerActionTypeId) REFERENCES BreakerActionTypes(Id),
    CONSTRAINT UQ_... UNIQUE(BreakerTypeId, BreakerActionTypeId)
);
```

**Scenario 3: Junction Table WITHOUT ID Column (Composite Primary Key)**
Use for pure many-to-many without additional junction properties. EF Core handles this with `HasMany().WithMany()`.

```csharp
// NO junction entity class needed!

// Parent Entity - direct navigation to target entities
public class BreakerTypes
{
    public int Id { get; set; }
    public string Name { get; set; }
    public virtual ICollection<BreakerActionTypes> AvailableActionTypes { get; set; }
}

// Target Entity - reverse navigation
public class BreakerActionTypes
{
    public int Id { get; set; }
    public string Name { get; set; }
    public virtual ICollection<BreakerTypes> BreakerTypes { get; set; }
}

// DbContext Configuration
modelBuilder.Entity<BreakerTypes>()
    .HasMany(bt => bt.AvailableActionTypes)
    .WithMany(bat => bat.BreakerTypes)
    .UsingEntity<Dictionary<string, object>>(
        "BreakerTypeActions",  // Junction table name
        j => j.HasOne<BreakerActionTypes>().WithMany().HasForeignKey("BreakerActionTypeId"),
        j => j.HasOne<BreakerTypes>().WithMany().HasForeignKey("BreakerTypeId"),
        j =>
        {
            j.HasKey("BreakerTypeId", "BreakerActionTypeId");
            j.ToTable("BreakerTypeActions");
        });

// Admin Model - property name MUST match the navigation property name
[AdminFieldCommon("Available Action Types")]
[AdminFieldMultiSelect(SelectSourceType.DbType, typeof(BreakerActionTypes), "Name", "Id", 
    storeAsJson: false)]  // relatedEntityIdProperty defaults to "Id" for direct many-to-many
public List<int>? AvailableActionTypes { get; set; }
```

```sql
-- Database: Junction table with composite PK (no ID column)
CREATE TABLE BreakerTypeActions
(
    BreakerTypeId       INT NOT NULL,
    BreakerActionTypeId INT NOT NULL,
    CONSTRAINT PK_BreakerTypeActions PRIMARY KEY(BreakerTypeId, BreakerActionTypeId),
    CONSTRAINT FK_... FOREIGN KEY(BreakerTypeId) REFERENCES BreakerTypes(Id),
    CONSTRAINT FK_... FOREIGN KEY(BreakerActionTypeId) REFERENCES BreakerActionTypes(Id)
);
```

#### MultiSelect Key Points

| Scenario | `storeAsJson` | `relatedEntityIdProperty` | Junction Entity |
|----------|---------------|---------------------------|-----------------|
| 1. JSON | `true` | N/A | None |
| 2. Junction with ID | `false` | Foreign key property name (e.g., `BreakerActionTypeId`) | Required |
| 3. Direct Many-to-Many | `false` | `"Id"` (default) | None (EF handles it) |

**Critical Rules:**
1. Model property name MUST match the entity's navigation property name
2. For Scenario 2, `relatedEntityIdProperty` must point to the foreign key on the junction entity
3. For Scenario 3, ensure `DbContext` is properly configured with `UsingEntity<Dictionary<string, object>>()`
4. The model property type should be `List<int>?` (or the appropriate ID type)

**Date/Time Fields:**
- `[AdminFieldDateTime]` - Date/time picker (Date, Time, DateTime modes)

**File Fields:**
- `[AdminFieldFile]` - File upload (supports multiple files, drag-drop, platform-specific)
- `[AdminFieldPicture]` - Image upload (supports format conversion, cropping, platform-specific)
- NOTE: The properties must have variable type of FileContainerCollection.

**Other Fields:**
- `[AdminFieldCheckbox]` - Boolean checkbox
- `[AdminFieldColorPicker]` - Color picker
- `[AdminFieldCoordinatePicker]` - Map-based coordinate picker
- `[AdminFieldExternalVideo]` - YouTube/Vimeo video embed

### Platform-Specific Uploads

Platforms enum supports multiple platforms (Flags enum):
- `Platforms.Desktop` (1)
- `Platforms.Tablet` (2)
- `Platforms.Mobile` (4)
- `Platforms.App` (8)
- `Platforms.Custom1` (16)
- `Platforms.Custom2` (32)
- `Platforms.Custom3` (64)

Usage:
```csharp
[AdminFieldPicture(null, platforms: Platforms.Desktop | Platforms.Mobile | Platforms.App)]
public string ImagePath { get; set; }
```

### Visual Organization Attributes

**Tabs:**
```csharp
[Tab("General")]
// ... properties ...
[EndTab]

[Tab("Media")]
// ... properties ...
[EndTab]
```

**Containers:**
```csharp
[Container("Basic Information", "Category details")]
// ... properties ...
[EndContainer]
```

**Field Width:**
- Use `FieldWidth` enum in `[AdminFieldCommon]`: `Auto`, `Quarter`, `Third`, `Half`, `TwoThirds`, `ThreeQuarters`, `Full`

### Complex Types & Repeaters

**Complex Types** are nested classes/objects that can be stored as JSON or as separate database entities.

**Single Complex Type (JSON or EF):**
```csharp
// JSON Storage (simple)
[AdminFieldCommon("Extra Data")]
[ComplexType(typeof(AdminItemsExtraData), storeAsJson: true)]
public AdminItemsExtraData ItemsExtraData { get; set; }

// EF Relationship (separate table)
[AdminFieldCommon("Extra Data")]
[ComplexType(typeof(AdminItemsExtraData), storeAsJson: false, relatedEntity: typeof(ItemsExtraData))]
public AdminItemsExtraData ItemsExtraData { get; set; }
```

**Repeater (List of Complex Types):**
```csharp
[AdminFieldCommon("Properties")]
[Repeater(typeof(ItemProperty), storeAsJson: true, maxItems: 10)]
public List<ItemProperty> Properties { get; set; }

// With EF relationship
[AdminFieldCommon("Sub Items")]
[Repeater(typeof(AdminSubItemModel), storeAsJson: false, relatedEntity: typeof(SubItem), maxItems: 10)]
public List<AdminSubItemModel> SubItems { get; set; }
```

**Key Parameters:**
- `storeAsJson: true` - Store as JSON string in database column
- `storeAsJson: false` + `relatedEntity` - Store as EF relationship (separate table with foreign key)
- `maxItems` - Maximum items allowed
- `minItems` - Minimum items required
- `allowReordering` - Enable drag-and-drop reordering
- `deleteOnRemove` - Delete related entities when removed (EF relationships)
- `cascadeDelete` - Cascade delete related entities

**DB-Related Entities:**
When using `relatedEntity` parameter, the system creates a separate table:
- The complex type class becomes a separate entity
- Foreign key relationship is automatically managed
- Use `deleteOnRemove: true` to delete child entities when removed from parent
- Use `cascadeDelete: true` for cascade deletion

**Complex Type Class Definition:**
```csharp
// This can be used in ComplexType or Repeater
public class ItemProperty : BaseAdminModel
{
    [AdminFieldCommon("Key", required: true)]
    [AdminFieldText(maxLength: 100)]
    public string Key { get; set; }

    [AdminFieldCommon("Value", required: true)]
    [AdminFieldText(maxLength: 100)]
    public string Value { get; set; }
}
```

### Conditional Visibility

**Simple Show/Hide:**
```csharp
[ShowIf("CategoryId", 1, 2, 4)]  // Show if CategoryId is 1, 2, or 4
[HideIf("IsHighlighted", false)] // Hide if IsHighlighted is false
```

**Advanced Visibility Rules (in Controller):**
```csharp
protected override void GetVisibilityRules(ConditionalVisibilityRules<AdminItemModel> rules)
{
    rules.AddGroup()
        .When(x => x.IsHighlighted && x.CategoryId.HasValue)
        .Show(x => x.Price, x => x.Weight, x => x.Properties);

    rules.AddGroup()
        .WhenProperty("Properties.Count", 0, ">")
        .ShowProperties("Properties.Key", "Properties.Value");
}
```

### Admin Controllers

**Base Controller Pattern:**
```csharp
public class AdminItemsController : DinoAdminBaseEntityController<AdminItemModel, Item, int>
{
    public AdminItemsController() : base("item") { }

    protected override async Task<AdminSegment> CreateAdminSegment()
    {
        return new AdminSegment
        {
            General = new AdminSegmentGeneral
            {
                Name = "Items",
                Priority = 20,
                MenuHeader = "Title"
            },
            UI = new AdminSegmentUI
            {
                Icon = "shopping-cart",
                IconType = IconType.PrimeIcons,
                ShowInMenu = true,
            }
        };
    }

    protected override async Task<ListDef> CreateListDef(string refId = null)
    {
        return new ListDef
        {
            Title = "Items List",
            AllowReOrdering = true,
            AllowAdd = true,
            AllowEdit = true,
            AllowDelete = true,
            ShowArchive = true,
            ShowDeleteConfirmation = true,
            
            // Import/Export Configuration (ask if needed when creating new entity)
            AllowExcelImport = true,                                    // Enable Excel/CSV import
            AllowedExportFormats = ExportFormat.Excel | ExportFormat.Csv | ExportFormat.Pdf,  // Enable export formats
            ExportFilename = "Items"                                    // Custom export filename (optional)
        };
    }

    protected override IQueryable<Artwork> GetFilteredData(string refId, bool? showArchive, bool? showDeleted,  ListRetrieveParams listParams)
    {
        // Add category filter rule to listParams
        listParams.AdvancedFilters.Add(new AdvancedListFilter
        {
            MatchAll = true,
            PropertyName = "CategoryId",
            Rules = new List<FilterRule>
            {
                new FilterRule
                {
                    Operator = FilterOperator.Equals,
                    Value = "2"
                }
            }
        });

        return base.GetFilteredData(refId, showArchive, showDeleted, listParams);
    }
}
```

**Key Overrides:**
- `CreateAdminSegment()` - Define menu item, icon, priority and headers
- `CreateListDef()` - Configure list view (columns, actions, filters)
- `GetVisibilityRules()` - Advanced conditional visibility
- `GetFilteredData()` - Customize base query to show only SOME data (partial listing)
- `CustomPreMapFromDbModel()` / `CustomPostMapFromDbModel()` - Custom mapping logic (on AdminModel level, not the controller)
- `CustomPreMapToDbModel()` / `CustomPostMapToDbModel()` - Custom mapping logic (on AdminModel level, not the controller)

**Menu Headers (Dynamic Submenu Grouping):**
You can create custom submenu headers in the admin menu by setting the `MenuHeader` property in your segment's `General` configuration. Headers work cumulatively - once set, all subsequent menu items will be grouped under that header until a new header is defined.

**Header Inheritance Rules:**
- Headers apply to the defining segment AND all subsequent segments until the next `MenuHeader` is encountered
- If a segment with `MenuHeader` is not visible due to permissions, the header still applies to subsequent visible segments
- Segments without `MenuHeader` inherit the current header from previous segments
- Use `SHARED.*` keys for translatable headers (e.g., `MenuHeader = "SHARED.Content"`)

**Example Menu Structure:**
```csharp
// Result: Items grouped under custom headers
Segment 1: MenuHeader = null          → "Admin" header (default)
Segment 2: MenuHeader = null          → "Admin" header (inherited)
Segment 3: MenuHeader = "Content"      → "Content" header
Segment 4: MenuHeader = null          → "Content" header (inherited)
Segment 5: MenuHeader = "System"       → "System" header
// Settings appear at bottom with their own header
```

**Partial Data Listing:**
To list a page with only SOME of its data, override `GetFilteredData()` in your controller:
```csharp
protected override IQueryable<Artwork> GetFilteredData(string refId, bool? showArchive, bool? showDeleted,  ListRetrieveParams listParams)
    {
        // Add category filter rule to listParams
        listParams.AdvancedFilters.Add(new AdvancedListFilter
        {
            MatchAll = true,
            PropertyName = "CategoryId",
            Rules = new List<FilterRule>
            {
                new FilterRule
                {
                    Operator = FilterOperator.Equals,
                    Value = "2"
                }
            }
        });

        return base.GetFilteredData(refId, showArchive, showDeleted, listParams);
    }
```
This allows you to filter the base query before pagination, sorting, and other operations are applied.

**Custom Query Parameters:**
The admin panel automatically sends all custom query parameters from the URL on every request. You can use these in your controller methods:
```csharp
[HttpPost]
public virtual async Task<JsonResult> List([FromBody] ListRetrieveParams listParams,
    [FromQuery] bool showArchive = false, 
    [FromQuery] bool showDeleted = false, 
    [FromQuery] string refId = null,
    [FromQuery] string customFilter = null)  // Custom query parameter
{
    // Access customFilter from URL query string
    // Example: /DinoAdmin/Items/List?customFilter=value1
    // The customFilter parameter will be available in your method
}
```

**refId and ParentReferenceColumn:**
The `refId` parameter is specifically designed to filter by a parent reference column. Mark the property that represents the parent relationship with `[ParentReferenceColumn]`:
```csharp
[AdminFieldCommon("Category", tooltip: "Item category")]
[AdminFieldSelect(SelectSourceType.DbType, typeof(ItemCategory), "Name", "Id")]
[ListSettings]
[ParentReferenceColumn]  // This property is used for refId filtering
public int? CategoryId { get; set; }
```

When navigating to `/DinoAdmin/Items/List?refId=5`, the list will automatically filter by `CategoryId == 5`. The `refId` is the primary mechanism for parent-child filtering in the admin panel.

**Multiple Menu Items with Pre-Filtering:**
To create multiple menu items that show the same list but with different filters (e.g., "All Items" and "Category 5 Items"), you have two options:

**Option 1: Separate Controllers (Recommended for different filters)**
Create a separate controller for each filtered view:
```csharp
// Main Items Controller - Shows all items
public class AdminItemsController : DinoAdminBaseEntityController<AdminItemModel, Item, int>
{
    public AdminItemsController() : base("item") { }
    
    protected override async Task<AdminSegment> CreateAdminSegment()
    {
        return new AdminSegment
        {
            General = new AdminSegmentGeneral { Name = "All Items", Priority = 20, MenuHeader = "Title" },
            UI = new AdminSegmentUI { ShowInMenu = true, Icon = "shopping-cart" }
        };
    }
}

// Filtered Items Controller - Shows only Category 5 items
public class AdminCategory5ItemsController : DinoAdminBaseEntityController<AdminItemModel, Item, int>
{
    public AdminCategory5ItemsController() : base("item") { }
    
    // Override to pre-filter by category 5
    protected override IQueryable<Artwork> GetFilteredData(string refId, bool? showArchive, bool? showDeleted, ListRetrieveParams listParams)
    {
        // Add category filter rule to listParams
        listParams.AdvancedFilters.Add(new AdvancedListFilter
        {
            MatchAll = true,
            PropertyName = "CategoryId",
            Rules = new List<FilterRule>
            {
                new FilterRule
                {
                    Operator = FilterOperator.Equals,
                    Value = "5"
                }
            }
        });

        return base.GetFilteredData(refId, showArchive, showDeleted, listParams);
    }
    
    protected override async Task<AdminSegment> CreateAdminSegment()
    {
        return new AdminSegment
        {
            General = new AdminSegmentGeneral { Name = "Category 5 Items", Priority = 21 },
            UI = new AdminSegmentUI { ShowInMenu = true, Icon = "tag" },
            Navigation = new AdminSegmentNavigation
            {
                ControllerName = "AdminCategory5Items",
                CustomPath = "AdminCategory5Items/List"
            }
        };
    }
}
```

**Option 2: Using CustomPath with Query Parameters**
Use `CustomPath` in `AdminSegmentNavigation` to add query parameters:
```csharp
protected override async Task<AdminSegment> CreateAdminSegment()
{
    return new AdminSegment
    {
        General = new AdminSegmentGeneral { Name = "Category 5 Items", Priority = 21 },
        UI = new AdminSegmentUI { ShowInMenu = true, Icon = "tag" },
        Navigation = new AdminSegmentNavigation
        {
            ControllerName = "AdminItems",
            CustomPath = "AdminItems/List?categoryId=5"  // Custom path with query parameter
        }
    };
}

// In your List method, handle the query parameter:
[HttpPost]
public virtual async Task<JsonResult> List([FromBody] ListRetrieveParams listParams,
    [FromQuery] bool showArchive = false,
    [FromQuery] bool showDeleted = false,
    [FromQuery] string refId = null,
    [FromQuery] int? categoryId = null)  // Custom filter from query string
{
    // The categoryId will be available from the URL query string
    // Use it in GetFilteredData or GetFilteredData
}

// Override GetFilteredData to use the query parameter:
protected override IQueryable<Artwork> GetFilteredData(string refId, bool? showArchive, bool? showDeleted, ListRetrieveParams listParams)
{
    // Add category filter rule to listParams
    listParams.AdvancedFilters.Add(new AdvancedListFilter
    {
        MatchAll = true,
        PropertyName = "CategoryId",
        Rules = new List<FilterRule>
        {
            new FilterRule
            {
                Operator = FilterOperator.Equals,
                Value = "2"
            }
        }
    });

    return base.GetFilteredData(refId, showArchive, showDeleted, listParams);
}
```

**Important Notes:**
- `refId` is specifically for parent-child relationships using `[ParentReferenceColumn]`
- For other filtering needs, use custom query parameters or override `GetFilteredData()`
- Custom query parameters from the URL are automatically available in controller methods
- Multiple menu items can point to the same controller with different query parameters

### Auto-Mapping

Auto-mapping between Admin Models and DB Entities is handled automatically by AutoMapper. Custom mapping can be done via methods on the AdminModel:

**Custom Mapping Methods (on AdminModel):**
Located in: `Submodules/Dino.CoreMvc.Admin/Models/Admin/BaseAdminModel.cs`

```csharp
public class AdminItemModel : BaseAdminModel
{
    // Called BEFORE mapping FROM database entity TO admin model
    // Return false to skip automatic mapping (you handle it manually)
    // Return true to continue with automatic mapping
    public override bool CustomPreMapFromDbModel(dynamic dbModel, dynamic model, ModelMappingContext context)
    {
        // You can cast dbModel to specific type if needed: var entity = (Item)dbModel;
        // Or use ExpandoObject for dynamic properties (see AdminBaseSettings example)
        
        // Example: Skip automatic mapping and handle manually
        if (someCondition)
        {
            model.CustomProperty = dbModel.SomeOtherProperty;
            return false; // Stop automatic mapping
        }
        
        return true; // Continue with automatic mapping
    }

    // Called AFTER mapping FROM database entity TO admin model
    // Use this to modify the model after automatic mapping is complete
    public override void CustomPostMapFromDbModel(dynamic dbModel, dynamic model, ModelMappingContext context)
    {
        // Modify model after automatic mapping
        model.CustomProperty = dbModel.SomeOtherProperty;
        model.ComputedValue = CalculateSomething(model);
    }

    // Called BEFORE mapping FROM admin model TO database entity
    // Return false to skip automatic mapping (you handle it manually)
    // Return true to continue with automatic mapping
    public override bool CustomPreMapToDbModel(dynamic model, dynamic dbModel, ModelMappingContext context)
    {
        // You can cast dbModel to specific type if needed: var entity = (Item)dbModel;
        // Or use ExpandoObject for dynamic properties (see AdminBaseSettings example)
        
        // Example: Skip automatic mapping and handle manually
        if (someCondition)
        {
            dbModel.SomeOtherProperty = model.CustomProperty;
            return false; // Stop automatic mapping
        }
        
        return true; // Continue with automatic mapping
    }

    // Called AFTER mapping FROM admin model TO database entity
    // Use this to modify the entity after automatic mapping is complete
    public override void CustomPostMapToDbModel(dynamic model, dynamic dbModel, ModelMappingContext context)
    {
        // Modify entity after automatic mapping
        dbModel.SomeOtherProperty = model.CustomProperty;
        dbModel.UpdateDate = DateTime.UtcNow;
    }
}
```

**Important Notes:**
- All parameters are `dynamic` - you can cast them to specific types when needed: `var entity = (Item)dbModel;`
- For complex scenarios with dynamic properties, use `ExpandoObject` (see `AdminBaseSettings` example)
- `ModelMappingContext` provides access to current user ID, mapping options, etc.
- Returning `false` from `CustomPreMap*` methods stops automatic mapping - you must handle everything manually
- Returning `true` from `CustomPreMap*` methods allows automatic mapping to continue
- `CustomPostMap*` methods are always called after automatic mapping (if it occurred)
- See `Submodules/Dino.CoreMvc.Admin/Models/Admin/AdminBaseSettings.cs` for a complex example using `ExpandoObject` and JSON serialization

**Skip Mapping:**
Use `[SkipMapping]` attribute to exclude properties from automatic mapping. Located in: `Submodules/Dino.CoreMvc.Admin/Attributes/SkipMappingAttribute.cs`

```csharp
// Skip mapping completely (both directions)
[SkipMapping]
public string ComputedProperty { get; set; }

// Skip only from database to model
[SkipMapping(skipFromDb: true)]
public string ReadOnlyFromDb { get; set; }

// Skip only from model to database
[SkipMapping(skipToDb: true)]
public string ComputedProperty { get; set; }

// Skip null values when mapping to database (preserve existing values)
[SkipMapping(skipNullValues: true)]
public string? OptionalField { get; set; }

// Skip during import/export operations
[SkipMapping(skipImport: true, skipExport: true)]
public string InternalField { get; set; }

// Full control example
[SkipMapping(
    skipFromDb: false,      // Map from DB to model
    skipToDb: true,         // Don't map from model to DB
    skipNullValues: true,   // Don't overwrite with null
    skipImport: true,       // Skip during import
    skipExport: false      // Include in export
)]
public string? ConditionalField { get; set; }
```

**SkipMapping Parameters:**
- `skipFromDb` (default: false) - Skip mapping from database entity to admin model
- `skipToDb` (default: false) - Skip mapping from admin model to database entity
- `skipNullValues` (default: false) - Skip mapping null values to existing properties in database entity (preserves existing values)
- `skipImport` (default: false) - Skip the property during import operations
- `skipExport` (default: false) - Skip the property during export operations

**Common Use Cases:**
- Computed/calculated properties: `[SkipMapping(skipToDb: true)]` - Read from DB but don't save
- Read-only properties: `[SkipMapping(skipFromDb: false, skipToDb: true)]` - Display but don't update
- Optional fields that shouldn't overwrite: `[SkipMapping(skipNullValues: true)]` - Only update if value provided
- Internal/system fields: `[SkipMapping(skipImport: true, skipExport: true)]` - Hide from import/export

### Import & Export Functionality

The admin panel includes built-in import and export functionality for Excel, CSV, and PDF formats.

**Export Formats:**
Located in: `Submodules/Dino.CoreMvc.Admin/Models/ListData.cs` (ExportFormat enum)
- `ExportFormat.Excel` - Excel format (.xlsx)
- `ExportFormat.Csv` - CSV format (.csv)
- `ExportFormat.Pdf` - PDF format (.pdf)
- `ExportFormat.None` - Disable export

**Import Formats:**
- Excel files (.xlsx, .xls)
- CSV files (.csv)

**Configuration in ListDef:**
```csharp
protected override async Task<ListDef> CreateListDef(string refId = null)
{
    return new ListDef
    {
        // ... other settings ...
        
        // Enable import (Excel/CSV)
        AllowExcelImport = true,
        
        // Enable export formats (Flags enum - can combine multiple)
        AllowedExportFormats = ExportFormat.Excel | ExportFormat.Csv | ExportFormat.Pdf,
        
        // Custom export filename (optional, defaults to controller name)
        ExportFilename = "Items"
    };
}
```

**SkipMapping for Import/Export:**
Use `[SkipMapping]` attribute to control which properties are included:
```csharp
// Skip property from import/export
[SkipMapping(skipImport: true, skipExport: true)]
public string InternalField { get; set; }

// Skip only from import
[SkipMapping(skipImport: true)]
public string AutoGeneratedField { get; set; }

// Skip only from export
[SkipMapping(skipExport: true)]
public string SensitiveData { get; set; }
```

**Import Process:**
1. Admin downloads template using `DownloadImportTemplate` endpoint
2. Template includes all properties except those marked with `[SkipMapping(skipImport: true)]`
3. Admin fills template with data
4. Admin uploads file via `Import` endpoint
5. System validates and imports data in batches (default batch size: 1000)
6. Returns import result with success/error counts

**Export Process:**
1. Admin selects export format from list view
2. System exports all list data (respects current filters)
3. Properties marked with `[SkipMapping(skipExport: true)]` are excluded
4. File is downloaded with timestamp in filename

**Permissions:**
- Import requires `PermissionType.Import` permission
- Export requires `PermissionType.Export` permission
- Both are checked automatically by the base controller

**When Creating a New Entity:**
Always ask if import/export support is needed:
- "Do you need import functionality? (Excel/CSV)"
- "Do you need export functionality? (Excel/CSV/PDF)"
- If yes, configure `AllowExcelImport` and `AllowedExportFormats` in `CreateListDef()`
- Mark internal/system properties with `[SkipMapping(skipImport: true, skipExport: true)]`

### Settings Models

**System Settings:**
```csharp
public class SystemSettings : SystemSettingsBase
{
    [AdminFieldCommon("Site Name", required: true)]
    [AdminFieldText(maxLength: 100)]
    public string SiteName { get; set; }
}
```

**Dino Master Settings (DinoAdmin role only - HIDDEN from regular admins):**
```csharp
[AdminPermission((short)RoleType.DinoAdmin)]
public class DinoMasterSettings : DinoMasterSettingsBase
{
    [AdminFieldCommon("Master Setting", required: true)]
    [AdminFieldText(maxLength: 100)]
    public string MasterSetting { get; set; }
}
```

**Settings Pattern:**
- Inherit from `SystemSettingsBase` or `DinoMasterSettingsBase`
- Use `[AdminPermission(RoleType.DinoAdmin)]` for master settings - these are HIDDEN from regular admins
- Properties use same admin field attributes as regular models
- Settings are managed through `AdminSettingsController` (auto-generated)
- **DinoMasterSettings are ONLY visible to DinoAdmin role** - use this to hide settings from developers/regular admins

### Permissions (AdminPermission)

**Permission Attribute:**
```csharp
[AdminPermission(
    MinimumRoleTypeRequired = (short)RoleType.DinoAdmin,  // Minimum role type (0=DinoAdmin, 1=Admin, etc.)
    AllowedRoleIdentifiers = new[] { 1, 2 },            // Specific role IDs
    FullAccess = true,                                    // Full access (overrides individual)
    CanView = true,                                       // Can view
    CanAdd = true,                                        // Can create
    CanEdit = true,                                       // Can update
    CanDelete = true,                                     // Can delete
    CanArchive = true,                                    // Can archive
    CanExport = true,                                     // Can export
    CanImport = true                                      // Can import
)]
public class AdminItemsController : DinoAdminBaseEntityController<AdminItemModel, Item, int>
{
}
```

**Role Types:**
- `RoleType.DinoAdmin` (0) - Master admin, highest privilege
- `RoleType.Admin` (1) - Regular admin
- Custom role types (2+) - Custom roles

**Permission Types:**
- `PermissionType.View` - View access
- `PermissionType.Add` - Create access
- `PermissionType.Edit` - Update access
- `PermissionType.Delete` - Delete access
- `PermissionType.Archive` - Archive access
- `PermissionType.Export` - Export access
- `PermissionType.Import` - Import access

**Usage Examples:**
```csharp
// Only DinoAdmin can access
[AdminPermission((short)RoleType.DinoAdmin)]

// Specific role IDs
[AdminPermission(AllowedRoleIdentifiers = new[] { 1, 2 }, CanView = true, CanEdit = true)]

// Minimum role type (role type <= specified value)
[AdminPermission(MinimumRoleTypeRequired = (short)RoleType.Admin, FullAccess = true)]
```

### Business Logic Layer (BL)

**Base BL Pattern:**
```csharp
public class ItemBL : BaseBL<MainDbContext, BlConfig, DinoCacheManager>
{
    public ItemBL(BLFactory<MainDbContext, BlConfig, DinoCacheManager> factory, MainDbContext context, IMapper mapper)
        : base(factory, context, mapper)
    {
    }

    // Custom business logic methods
    public async Task<Item> GetItemAsync(int id)
    {
        return await Db.Items.FindAsync(id);
    }
}
```

**Key Features:**
- Access to `Db` (DbContext), `Cache` (Cache Manager), `BlConfig`, `Mapper`
- `SaveChanges()` / `SaveChangesAsync()` - Save with automatic after-save actions
- `RegisterAfterSuccessfulSaveAction()` - Register actions to run after save

### Cache Management

**Cache Manager:**
```csharp
public class DinoCacheManager : BaseDinoCacheManager<MainDbContext, BlConfig, DinoCacheManager>
{
    public DinoCacheManager(IConfiguration config, IMapper mapper, IOptions<BlConfig> blConfig, IServiceProvider serviceProvider)
        : base(config, mapper, blConfig, serviceProvider)
    {
    }
}
```

**Cache Configuration (appsettings.json):**
```json
"BlConfig": {
    "CacheConfig": {
        "UseRedis": false,  // IMPORTANT: Redis is OPTIONAL, usually not used
        "RedisHost": "localhost",
        "RedisPort": 6379,
        "RedisPassword": "",
        "RedisDatabase": 0,
        "DefaultExpiration": "00:30:00"
    }
}
```

**Cache Model Attribute:**
```csharp
[CacheModel(
    DbModelType = typeof(Item),                    // Required: DB entity type
    CacheTiming = CacheTiming.OnApplicationStart, // When to load: OnFirstAccess or OnApplicationStart
    Expiration = 60,                                // Custom expiration (seconds)
    UseSlidingExpiration = false,                  // Sliding vs absolute expiration
    ManualMapping = false,                         // Skip auto-mapping if true
    CacheOnCreate = true,                          // Cache when created
    UpdateOnEdit = true,                            // Update cache on edit
    RemoveOnDelete = true,                          // Remove from cache on delete
    ReloadOnSort = true,                            // Reload cache on sort change
    UseRedis = true                                // Use Redis for this model (if Redis enabled)
)]
public class ItemCacheModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    // Only include frequently accessed, stable properties
}
```

**Cache Timing:**
- `CacheTiming.OnFirstAccess` - Load when first accessed (lazy loading)
- `CacheTiming.OnApplicationStart` - Load on application startup (eager loading)

**Cache Attributes:**
- Use `[CacheModel]` attribute on cache model classes (not admin models)
- Cache models should be simplified versions of entities (only frequently accessed properties)
- Cache is automatically managed by `BaseDinoCacheManager`

**Cache Usage:**
All cache retrieval and expiration logic is handled inside `DinoCacheManager`. When the BL needs cached data, it calls the cache manager, which returns the object from cache (if found and valid) or loads and caches it if needed.

**Manual Cache Implementation (Recommended for Complex Scenarios):**
For scenarios where you need to cache all elements of a given entity (such as the full collection of items), and require full control over cache keys, data loading, and cache invalidation, implement manual caching using `_cacheManager.GetOrCreateByKeyOnlyAsync()`:

```csharp
#region Collection Cache Methods

/// <summary>
/// Get all items from cache
/// Cache key: "Items:All"
/// </summary>
public async Task<List<ItemCacheModel>> GetItems()
{
    return await _cacheManager.GetOrCreateByKeyOnlyAsync(
        "Items:All",
        async (entry) =>
        {
            var db = GetNewDbContext();

            var items = await db.Items
                .Where(i => !i.IsDeleted)
                .OrderBy(i => i.SortIndex)
                .ThenBy(i => i.Name)
                .ToListAsync();

            return _mapper.Map<List<Item>, List<ItemCacheModel>>(items);
        }, new MemoryCacheEntryOptions());
}

/// <summary>
/// Invalidate items cache
/// </summary>
public void InvalidateItems()
{
    _cacheManager.RemoveByKeyOnly("Items:All");
}

#endregion
```

**Key Points for Manual Cache Implementation:**
1. **Use `_mapper.Map`** - Not `Mapper.Map`
2. **Use `GetNewDbContext()`** - For database access in cache methods
3. **Cache keys should be descriptive** - Format: `"EntityName:All"`
4. **Include related entities using `Include()` and `ThenInclude()`** when needed
5. **Filter by `!IsDeleted`** - Always exclude soft-deleted items
6. **Order by `SortIndex` then by name** - For consistent ordering

**Cache Invalidation in Controllers:**
Use the proper override methods in admin controllers to invalidate cache when entities change:

```csharp
// In AdminItemsController.cs
protected override async Task OnEntityCreatedForCache(AdminItemModel adminModel, Item efModel, object id)
{
    await base.OnEntityCreatedForCache(adminModel, efModel, id);
    ((DinoCacheManager)DinoCacheManager).InvalidateItems();
}

protected override async Task OnEntityUpdatedForCache(AdminItemModel adminModel, Item efModel, object id)
{
    await base.OnEntityUpdatedForCache(adminModel, efModel, id);
    ((DinoCacheManager)DinoCacheManager).InvalidateItems();
}

protected override async Task OnEntityDeletedForCache(object id)
{
    await base.OnEntityDeletedForCache(id);
    ((DinoCacheManager)DinoCacheManager).InvalidateItems();
}

protected override async Task OnSortChangedForCache(IEnumerable<int> sortedIds)
{
    await base.OnSortChangedForCache(sortedIds);
    ((DinoCacheManager)DinoCacheManager).InvalidateItems();
}
```

**Cache Model Location:**
- Cache models go in: `DinoGenericAdmin.BL/Cache/Models/`
- **NOT** in `DinoGenericAdmin.BL/Models/` (that's for entity models)

**AutoMapper Configuration:**
Add mappings in `DinoGenericAdmin.BL/Cache/BLAutoMapperProfile.cs`:
```csharp
// In BLAutoMapperProfile.cs
CreateMap<EntityModel, CacheModel>();
CreateMap<RelatedEntity, RelatedCacheModel>();
```

**Admin AutoMapper Configuration:**
For Admin models, the mappings are handled automatically. However, if you need custom mappings for complex scenarios, check `DinoGenericAdmin.Api/Logic/Converters/AutoMapperConfig.cs`:
```csharp
// In MapperProfile.cs (inherits AdminBaseMapperProfile)
public MapperProfile(IServiceProvider serviceProvider)
    : base(serviceProvider)
{
    // Add custom mappings here if needed
    // Most mappings are handled automatically by the base class
}
```

**CRITICAL**: Cache models are separate from Admin models. Cache models should only contain frequently accessed, lightweight properties.

### Hangfire Integration

**Setup (Program.cs):**
```csharp
// Uncomment this block to enable Hangfire services:
builder.Services.AddDinoHangfire(builder.Configuration, "Hangfire");

// Register regular (non-recurring) job
builder.Services.AddHangfireJob<DemoCleanupJob>();

// Register simple recurring job
builder.Services.AddHangfireJob<DemoEmailReminderJob>();

// Register complex job with dependencies
builder.Services.AddHangfireJob<ExampleComplexJob>(provider => new ExampleComplexJob(
    provider.GetRequiredService<ILogger<ExampleComplexJob>>(),
    provider.GetRequiredService<BLFactory<MainDbContext, BlConfig, DinoCacheManager>>(),
    provider.GetRequiredService<DinoCacheManager>()
));

// In app configuration:
// Uncomment this line to enable Hangfire dashboard and job initialization:
app.UseDinoHangfire();
```

**Configuration (appsettings.json):**
```json
"Hangfire": {
    "ConnectionString": "...",
    "DashboardAllowedIps": ["127.0.0.1", "::1"],
    "EnableProcessing": true,
    "EnableDashboard": true,
    "DashboardPath": "/hangfire",
    "Queues": ["critical", "emails", "default"],
    "CompatibilityLevel": 180,
    "CreateDatabaseTablesIfNotExist": true
}
```

**Regular Job Pattern (non-recurring):**
```csharp
public class DemoCleanupJob : BaseHangfireJob
{
    public DemoCleanupJob(ILogger<DemoCleanupJob> logger)
        : base(logger)
    {
    }

    public override string JobName => "DemoCleanupJob";
    public override string Queue => "default";

    protected override Task ExecuteJobAsync()
    {
        // One-time/manual job implementation
        return Task.CompletedTask;
    }
}

// Trigger manually when needed:
BackgroundJob.Enqueue<JobScheduler>(x => x.ExecuteJob(typeof(DemoCleanupJob)));
```

**Recurring Job Pattern:**
```csharp
public class DemoEmailReminderJob : BaseRecurringHangfireJob
{
    public DemoEmailReminderJob(ILogger<DemoEmailReminderJob> logger)
        : base(logger)
    {
    }

    public override string JobName => "DemoEmailReminder";
    public override string CronSchedule => "0 9 * * *"; // Daily at 9 AM
    public override string Queue => "emails";
    
    protected override Task ExecuteJobAsync()
    {
        // Job implementation
        return Task.CompletedTask;
    }
}
```

**Complex Job Pattern (with dependencies):**
```csharp
public class ExampleComplexJob : BaseHangfireJob
{
    private readonly BLFactory<MainDbContext, BlConfig, DinoCacheManager> _blFactory;
    private readonly DinoCacheManager _dinoCacheManager;

    public ExampleComplexJob(
        ILogger<ExampleComplexJob> logger,
        BLFactory<MainDbContext, BlConfig, DinoCacheManager> blFactory,
        DinoCacheManager dinoCacheManager) 
        : base(logger)
    {
        _blFactory = blFactory;
        _dinoCacheManager = dinoCacheManager;
    }

    public override string JobName => "ComplexBusinessProcessJob";
    public override string Queue => "critical";

    protected override async Task ExecuteJobAsync()
    {
        // Use _blFactory to get BL services
        // Use _dinoCacheManager for cache operations
        // Implement job logic
    }
}
```

**Job Registration:**
- Regular (non-recurring) jobs: Inherit `BaseHangfireJob` or implement `IHangfireJob`
- Recurring jobs: Inherit `BaseRecurringHangfireJob` or implement `IRecurringHangfireJob`
- Use `AddHangfireJob<TJob>()` (jobs are registered as scoped)
- `CronSchedule` belongs only to recurring jobs
- For auto-registration by `JobScheduler`, the job must use the recurring base/interface
- Jobs are automatically registered and scheduled on startup

### File Uploads

**Configuration:**
- Files stored in `wwwroot/uploads` (configurable via `ApiConfig.UploadsFolder`)
- Supports Azure Blob Storage (configure via `BlConfig.StorageConfig`)
- Platform-specific uploads: `Platforms.Desktop | Platforms.Mobile | Platforms.Tablet | Platforms.App | Platforms.Custom1-3`

**File Container:**
```csharp
[AdminFieldFile(new[] { "pdf", "doc", "docx" }, maxSize: 10)]
public FileContainerCollection DocumentPath { get; set; }
```

### Multi-Language Support

**Admin Panel UI Language (Layout):**
The `AllowHebrew` and `AllowEnglish` settings in `AdminConfig` control the admin panel's visual layout (RTL/LTR, UI language), **NOT** content translations:
```json
"AdminConfig": {
    "AllowHebrew": true,   // Enable Hebrew UI layout (RTL)
    "AllowEnglish": true   // Enable English UI layout (LTR)
}
```
These settings determine which UI languages are available for the admin panel interface itself (menus, buttons, labels).

**Content Multi-Language (Translations):**
For translating actual content (entity properties), use the `[MultiLanguage]` attribute:
```csharp
[AdminFieldCommon("Description")]
[AdminFieldRichText]
[MultiLanguage]
public Dictionary<string, string> DescriptionMultiLanguage { get; set; }
```

**Key Points:**
- Use `[MultiLanguage]` attribute on properties for content translations
- Property type MUST be `Dictionary<string, T>` where:
  - Key: Language code (e.g., "en", "he", "fr", "de" - any language codes you need)
  - Value: The actual value type (string, FileContainerCollection, etc.)
- Supported value types: `string`, `FileContainerCollection` (for file uploads), and other types
- Content translations are **independent** of `AllowHebrew`/`AllowEnglish` settings
- You can have content in any languages regardless of UI language settings
- Example with file uploads:
```csharp
[AdminFieldCommon("Document")]
[AdminFieldFile(new[] { "pdf" })]
[MultiLanguage]
public Dictionary<string, FileContainerCollection> DocumentMultiLanguage { get; set; }
```

**Important Distinction:**
- `AllowHebrew`/`AllowEnglish` = Admin panel UI language (layout, RTL/LTR)
- `[MultiLanguage]` attribute = Content translations (entity property values in multiple languages)

### Archive vs Delete

**Archive:**
- Archive is DIFFERENT from deletion
- Uses `Archived` property (BIT NOT NULL DEFAULT 0)
- Requires `CanArchive` permission
- Archived items are hidden from normal list view but can be shown with `ShowArchive = true`
- Use `[ArchiveIndicator]` attribute on the `Archived` property
- Archive functionality is controlled by `ShowArchive` in `ListDef` and `CanArchive` permission

**Soft Delete:**
- Uses `IsDeleted` property (BIT NOT NULL DEFAULT 0)
- Soft deleted items are hidden from normal queries
- Use `[DeletionIndicator]` attribute on the `IsDeleted` property
- When implementing deletion, ALWAYS ask if soft delete is wanted (if not mentioned)

**Hard Delete:**
- Permanently removes records from database
- No `IsDeleted` property needed
- Use when soft delete is not desired

**When Implementing Deletion:**
- If not specified, ASK: "Do you want soft delete (IsDeleted flag) or hard delete (permanent removal)?"
- If soft delete: Ensure `IsDeleted BIT NOT NULL DEFAULT 0` exists in DB
- If hard delete: No special property needed

### List Actions

**List Actions Configuration:**
```csharp
protected override async Task<ListDef> CreateListDef(string refId = null)
{
    return new ListDef
    {
        Title = "Items List",
        // ... other settings ...
        
        // Actions available from this list (navigate to other lists)
        Actions = new List<ListAction>
        {
            new ListAction(
                ListActionType.List,                    // Action type
                "View Categories",                      // Button text
                (new AdminCategoriesController()).Id,   // Target controller ID
                true,                                   // Pass entity ID
                icon: "tags",                          // Optional icon
                iconType: IconType.PrimeIcons           // Icon type
            ),
            new ListAction(
                ListActionType.Custom,                 // Custom action
                "Export Data",                         // Button text
                Id,                                    // This controller ID
                false,                                 // Don't pass entity ID
                actionName: nameof(ExportAction),      // Method name
                requireConfirmation: true,             // Require confirmation
                confirmationDialog: new DialogStructure
                {
                    Title = "Export Data?",
                    Description = "This will export all items"
                }
            ),
            new ListAction(
                ListActionType.OuterLink,              // External link
                "External Site",
                "https://example.com",
                false
            )
        },
        
        // Self actions (actions on selected items)
        SelfActions = new List<ListAction>
        {
            new ListAction(
                ListActionType.Custom,
                "Bulk Update",
                Id,
                false,
                actionName: nameof(BulkUpdateAction),
                passItemSelection: true,                // Pass selected items
                itemSelectionPropertyName: "ids"        // Property name for IDs
            )
        }
    };
}
```

**List Action Types:**
- `ListActionType.List` - Navigate to another list
- `ListActionType.Custom` - Custom controller action
- `ListActionType.Edit` - Navigate to edit page
- `ListActionType.OuterLink` - External URL

**List Action Parameters:**
- `type` - Action type
- `text` - Button text
- `segmentId` - Target controller ID or URL
- `passEntityId` - Whether to pass entity ID
- `actionName` - Method name for custom actions
- `icon` / `iconType` - Icon configuration
- `redirect` - Whether to redirect after action
- `requireConfirmation` - Show confirmation dialog
- `confirmationDialog` - Dialog structure
- `passModelToConfirmation` - Pass model to confirmation
- `passItemSelection` - Pass selected items
- `itemSelectionPropertyName` - Property name for selected IDs
- `reloadData` - Reload list after action
- `showSuccessMessage` - Show success message
- `responseType` - Response type (Json, File, Redirect)

**Custom Action Implementation:**
```csharp
[HttpPost]
public async Task<JsonResult> ExportAction([FromBody] AdminItemModel model)
{
    // Custom logic
    return CreateJsonResponse(true, result, null);
}

[HttpPost]
public async Task<JsonResult> BulkUpdateAction([FromBody] BulkUpdateRequest request)
{
    // request.ids contains selected item IDs
    // Custom logic
    return CreateJsonResponse(true, null, null);
}
```

## Code Style & Conventions

### C# Conventions
- Use PascalCase for classes, methods, properties
- Use camelCase for local variables and parameters
- Use `async`/`await` for all async operations
- Use `nameof()` for property references in attributes
- Use nullable reference types where appropriate

### Attribute Usage
- Always provide `name` parameter in `[AdminFieldCommon]`
- Use `tooltip` for helpful descriptions
- Set `required: true` for mandatory fields
- Use `readOnly: true` for computed/audit fields
- Use `[ListSettings]` to control list column appearance

### Naming Conventions
- Admin Models: `Admin{EntityName}Model` (e.g., `AdminItemModel`)
- Controllers: `Admin{EntityName}Controller` (e.g., `AdminItemsController`)
- BL Classes: `{EntityName}BL` (e.g., `ItemBL`)
- Entities: `{EntityName}` (e.g., `Item`)
- Cache Models: `{EntityName}CacheModel` (e.g., `ItemCacheModel`)

### File Organization
- Controllers: `Areas/Admin/Controllers/` (root level, no subfolders except Demo/Basics)
- Models: `Areas/Admin/Models/` (root level, no subfolders except Demo/Basics)
- Settings: `ModelsSettings/`
- BL Classes: `BL/` (root level, no subfolders except Demo/Basics)
- BL Models: `BL/Models/` (root level, no subfolders except Demo/Basics)
- Entities: `BL/Data/` or `BL/Models/`

## Common Tasks

### Adding a New Entity

1. **Create Entity Class** (in BL/Data or BL/Models):
```csharp
public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // Default properties (ask if we want to use defaults):
    public bool IsDeleted { get; set; } = false;        // Soft delete
    public int SortIndex { get; set; } = 0;             // Sorting (SUPER IMPORTANT)
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdateDate { get; set; } = DateTime.UtcNow;
    public int UpdateBy { get; set; } = 1;
    
    // DO NOT include Archive by default
    // public bool Archived { get; set; } = false;  // Only if needed
}
```

2. **Create Admin Model** (with default properties):
```csharp
public class AdminItemModel : BaseAdminModel
{
    [AdminFieldCommon("ID", readOnly: true)]
    [AdminFieldNumber]
    [VisibilitySettings(showOnCreate: false)]
    [ListSettings]
    public int Id { get; set; }

    [AdminFieldCommon("Name", required: true)]
    [AdminFieldText(maxLength: 100)]
    [ListSettings]
    public string Name { get; set; }

    // Default properties with proper attributes
    [AdminFieldCommon("Deleted", readOnly: true)]
    [AdminFieldCheckbox]
    [VisibilitySettings(false)]
    [DeletionIndicator]
    public bool IsDeleted { get; set; }

    [AdminFieldCommon("Order", readOnly: true)]
    [AdminFieldNumber]
    [SortIndex]
    [VisibilitySettings(false)]
    [ListSettings]
    public int SortIndex { get; set; }

    [AdminFieldCommon("Created Date", readOnly: true)]
    [AdminFieldDateTime]
    [VisibilitySettings(false)]
    [SaveDate]
    public DateTime CreateDate { get; set; }

    [AdminFieldCommon("Last Updated", readOnly: true)]
    [AdminFieldDateTime]
    [VisibilitySettings(false)]
    [LastUpdateDate]
    public DateTime UpdateDate { get; set; }

    [AdminFieldCommon("Updated By", readOnly: true)]
    [AdminFieldNumber]
    [VisibilitySettings(false)]
    [UpdatedBy]
    public int UpdateBy { get; set; }
}
```

3. **Create Controller**:
```csharp
public class AdminItemsController : DinoAdminBaseEntityController<AdminItemModel, Item, int>
{
    public AdminItemsController() : base("item") { }
    
    protected override async Task<AdminSegment> CreateAdminSegment() { /* ... */ }
    
    protected override async Task<ListDef> CreateListDef(string refId = null)
    {
        return new ListDef
        {
            Title = "Items List",
            AllowAdd = true,
            AllowEdit = true,
            AllowDelete = true,
            
            // Ask: Do you need import/export functionality?
            AllowExcelImport = true,  // Enable if import needed
            AllowedExportFormats = ExportFormat.Excel | ExportFormat.Csv | ExportFormat.Pdf,  // Enable if export needed
            ExportFilename = "Items"  // Optional custom filename
        };
    }
}
```

**Important Questions When Creating New Entity:**
- Do you need import functionality? (Excel/CSV import)
- Do you need export functionality? (Excel/CSV/PDF export)
- Which properties should be excluded from import/export? (Use `[SkipMapping(skipImport: true, skipExport: true)]`)

4. **Register in DbContext** (if needed):
```csharp
public DbSet<Item> Items { get; set; }
```

5. **Configure Import/Export (if needed)**:
   - Ask: "Do you need import functionality? (Excel/CSV)"
   - Ask: "Do you need export functionality? (Excel/CSV/PDF)"
   - If yes, configure in `CreateListDef()`:
     ```csharp
     AllowExcelImport = true,
     AllowedExportFormats = ExportFormat.Excel | ExportFormat.Csv | ExportFormat.Pdf,
     ExportFilename = "Items"  // Optional
     ```
   - Mark internal/system properties with `[SkipMapping(skipImport: true, skipExport: true)]`

6. **Provide Database Scripts**:
   - ALTER script for existing database
   - CREATE TABLE script for DbCreation.sql
   - Include all special properties with proper defaults

### Adding Custom Endpoints

Add methods to controller:
```csharp
[HttpPost]
public async Task<JsonResult> CustomAction([FromBody] AdminItemModel model)
{
    // Custom logic
    return CreateJsonResponse(true, result, null);
}
```

Reference in `ListDef.SelfActions` or `ListDef.Actions`.

### Adding Settings

1. Extend `SystemSettingsBase` or `DinoMasterSettingsBase`
2. Add properties with admin attributes
3. Settings automatically available in admin panel
4. Use `[AdminPermission(RoleType.DinoAdmin)]` to hide from regular admins

### Enum Usage

Enums can be used with `[AdminFieldSelect]`:
```csharp
public enum ItemStatus
{
    [Description("Available")]
    Available = 1,
    [Description("Out of Stock")]
    OutOfStock = 2,
    [Description("Discontinued")]
    Discontinued = 3
}

[AdminFieldCommon("Status")]
[AdminFieldSelect(SelectSourceType.Enum, typeof(ItemStatus))]
public ItemStatus Status { get; set; }
```

**Special Enum Features:**
- Use `[Description]` attribute for display text
- Enum values are automatically converted to select options
- Enum name is used as value, Description is used as display text

## Demo Folders Cleanup

**When starting a new project, DELETE demo content:**

1. **Delete Demo Folders:**
   - `ProjectName.Api/Areas/Admin/Controllers/Demo/`
   - `ProjectName.Api/Areas/Admin/Models/Demo/`
   - `ProjectName.BL/Models/Demo/`
   - `ProjectName.BL/Demo/` (if exists)

2. **Keep Basics Folders:**
   - `ProjectName.Api/Areas/Admin/Controllers/Basics/` - Required admin user/role controllers
   - `ProjectName.Api/Areas/Admin/Models/Basics/` - Required admin user/role models
   - `ProjectName.BL/Models/Basics/` - Required admin user/role entities

3. **Clean Up Program.cs:**
   - Remove demo job registrations
   - Remove demo BL service registrations

4. **Clean Up appsettings.json:**
   - Update connection strings
   - Configure for your environment

## Clean Admin Project Command

When asked to "clean admin project" or "set up new project", ask these questions:

1. **Project Name**: What is the project name? (replaces DinoGenericAdmin)
2. **Database**: 
   - Connection string?
   - Database name?
3. **Hangfire**: 
   - Use Hangfire? (yes/no)
   - If yes: uncomment the Hangfire registration and middleware lines in `Program.cs`
   - If yes: Connection string, dashboard path, queues, enable processing?
4. **Cache**:
   - Use Redis? (yes/no - usually no)
   - If yes: Redis host, port, password, database?
   - Default expiration time?
5. **Azure Blob Storage**:
   - Use Azure Blob Storage? (yes/no)
   - If yes: Connection string, container name, base URL?
6. **File Uploads**:
   - Change uploads folder path? (if yes, ask to what, otherwise keep current "uploads")
7. **CORS**:
   - Change CORS origins? (if yes, ask for semicolon-separated origins, otherwise keep current)
8. **Admin Panel UI Languages**:
   - Enable Hebrew UI layout (RTL)? (yes/no)
   - Enable English UI layout (LTR)? (yes/no)
   - Note: These control the admin panel's visual layout, NOT content translations
9. **OTP**:
   - Require OTP on login? (yes/no, default is yes)

Then implement:
- Rename all DinoGenericAdmin references to ProjectName
- **CRITICAL**: Update MainDbContext.cs (remove demo entities, add project entities)
- **CRITICAL**: Delete ALL demo folders: `Vitrea.BL/Demo/`, `Vitrea.BL/Models/Demo/`
- Update appsettings.json with provided values
- Update Program.cs configuration
- Remove demo job registrations
- Configure cache (Redis if requested)
- Configure Azure Blob Storage (if requested)
- Set up CORS (keep current if not changed)
- Configure languages
- Set logging environment name according to project name
- Set up OTP (default yes if not specified)

## Configuration Files

### appsettings.json Structure
- `ConnectionStrings.MainDbContext` - Database connection
- `Hangfire` - Hangfire configuration (if enabled)
- `AdminConfig` - Admin panel settings (OTP, UI languages)
  - `AllowHebrew` / `AllowEnglish` - Admin panel UI language support (layout/RTL/LTR, NOT content translations)
  - `LoginSecurityConfig` - OTP settings
- `ApiConfig` - API settings
  - `ClientBaseUrl` - Frontend URL
  - `ApiBaseUrl` - API base URL
  - `UploadsFolder` - File uploads folder
  - `AllowCorsOrigins` - CORS allowed origins (semicolon-separated)
  - `DateTimeStringFormat` - DateTime format
- `BlConfig` - Business logic config
  - `DebugMode` - Debug mode flag
  - `EmailsConfig` - Email settings (SMTP)
  - `StorageConfig` - Azure Blob Storage config
  - `CacheConfig` - Cache configuration (Redis optional)

**CRITICAL**: `ApiConfig` and `BlConfig` must ALWAYS be in sync:
- Every change in `appsettings.json` must be reflected in the config classes
- Every change in config classes must be reflected in `appsettings.json`
- Access configs from controllers and BL using base classes: `ApiConfig`, `BlConfig`

### Program.cs Setup
- Register DbContext
- Register Admin Services (`AddAdminServices`)
- Register Cache Manager
- Register AutoMapper profiles
- Configure Hangfire (if enabled)
- Configure CORS
- Configure authentication/authorization
- Initialize cache on startup
- Initialize file uploads
- Initialize settings

### App Controllers (Non-Admin)

**App controllers** (non-admin controllers) MUST inherit from `MainAppBaseController<T>`:
```csharp
public class HomeController : MainAppBaseController<HomeController>
{
    // Access configs via: ApiConfig, BlConfig
    // Access cache via: DinoCacheManager
    // Access BL factory via: BLFactory
    // Access logger via: Logger
}
```

**Settings and Cache Access:**
```csharp
// Get settings
var settings = AppSettings.Get<SystemSettings>();
var masterSettings = AppSettings.Get<DinoMasterSettings>();

// Get cache
var item = await DinoCacheManager.GetOrCreate<ItemCacheModel, int>(itemId);

// Register for settings change notifications
AppSettings.OnChanged<SystemSettings>(() => 
{
    Logger.LogInformation("Settings were updated");
});
```

## Important Notes

### DinoAdmin Role
- Special hidden role with elevated permissions (RoleType = 0)
- Required for accessing `DinoMasterSettings`
- Use `[AdminPermission(RoleType.DinoAdmin)]` to restrict access
- DinoMasterSettings are HIDDEN from regular admins - use for developer-only settings

### Auto-Mapping
- Models automatically map to/from EF entities using AutoMapper
- Override `CustomPreMap*` / `CustomPostMap*` methods on AdminModel for custom logic
- Use `[SkipMapping]` to control mapping behavior with granular options (see SkipMapping section above for all parameters)
- For file upload fields stored as `string` in the DB (but originally a FileContainerCollection): use `FileCollectionForClient` in the client model. The automapper is automatically taken care of.

### List View Configuration
- Properties with `[ListSettings]` appear in list view
- Use `[SortIndex]` for drag-and-drop reordering - **SUPER IMPORTANT**
- Use `[ArchiveIndicator]` / `[DeletionIndicator]` for soft delete UI
- Sorting requires `SortIndex INT NOT NULL DEFAULT 0` in database

### Conditional Visibility
- Simple: Use `[ShowIf]` / `[HideIf]` attributes
- Complex: Override `GetVisibilityRules()` in controller

### Tabs, Containers and sections
- The system supports tabs, containers and sections for organizing form fields
- Opening tab/container: [Tab("Name")] or [Container("Title", "Description")] or [Section("Title")] goes as the FIRST attribute on the first property in that tab/container
- Closing tab/container: [EndTab] or [EndContainer] or [EndSection] goes as the FIRST attribute on the LAST property in that tab/container (before [AdminFieldCommon])

Example:
```
[Tab("Basic Info")]
[AdminFieldCommon("Name")]  // First property in tab
public string Name { get; set; }

[AdminFieldCommon("Email")]
public string Email { get; set; }

[EndTab]  // FIRST attribute on last property in tab
[AdminFieldCommon("Phone")]  // This property is NOT in the tab
public string Phone { get; set; }
```

- Tabs and containers can nest (tab → container → container → section, or tab → section, etc)
- You can use tabs only, containers only, sections only, or all together
- If a property is alone inside a container or tab, it will have both [Container("Name")] and [EndContainer] on it, like this:
```
[Container("Basic Info")]
[EndContainer]
[AdminFieldCommon("Name")]  // First property in tab
public string Name { get; set; }
```
- Properties after [EndTab]/[EndContainer]/[EndSection] are outside of any tab/container structure - they belong to the parent scope (usually the class root level, not in any tab).


### Platform-Specific Uploads
- Use `platforms` parameter in `[AdminFieldFile]` / `[AdminFieldPicture]`
- Supports: `Platforms.Desktop | Platforms.Mobile | Platforms.Tablet | Platforms.App | Platforms.Custom1-3`

### Entity Classes
- Entity classes are pure database models (EF Core entities)
- Located in `BL/Data/` or `BL/Models/`
- These are the actual database tables
- Admin Models map to/from these entities

### Special Database Properties

**CRITICAL**: The following properties must have EXACT naming in the database for framework features to work:

**Required Properties (if using features):**
- `Archived` - BIT NOT NULL DEFAULT 0 (for archive functionality)
- `IsDeleted` - BIT NOT NULL DEFAULT 0 (for soft delete functionality)
- `Active` - BIT NOT NULL DEFAULT 1 (for active/inactive filtering)
- `CreateDate` - DATETIME NOT NULL DEFAULT GETDATE() (for creation timestamp)
- `UpdateDate` - DATETIME NOT NULL DEFAULT GETDATE() (for last update timestamp)
- `UpdateBy` - INT NOT NULL DEFAULT 1 (for tracking who updated)
- `SortIndex` - INT NOT NULL DEFAULT 0 (for drag-and-drop sorting - SUPER IMPORTANT)
- `LastLoginDate` - DATETIME NULL (for user login tracking)
- `LastIpAddress` - VARCHAR(50) NULL (for user IP tracking)

**When Implementing Special Properties:**
1. Check if the property exists in the DB model
2. If missing, ADD it to both DB model and Admin model
3. Configure it with proper attributes (`[ArchiveIndicator]`, `[DeletionIndicator]`, `[SortIndex]`, `[SaveDate]`, `[LastUpdateDate]`, `[UpdatedBy]`)
4. Provide TWO SQL scripts:
   - **ALTER script** for updating existing database
   - **CREATE TABLE script** for DbCreation.sql file

**Default Properties for New Admin Models:**
When creating a new model for the admin, by default (ask if we want to use defaults):
- ✅ Soft delete (`IsDeleted`) - Hidden.
- ✅ Sorting (`SortIndex`) - Hidden.
- ✅ CreateDate - Visible in list, hidden in editing.
- ✅ UpdateDate - Hidden.
- ✅ UpdateBy - Hidden.
- ❌ Archive (`Archived`) - DO NOT include by default. Hidden in list, visible and editable in editing.

**Admin Models Containers:**
For long admin models that contain lots of properties, gather them in logical sense and use containers for them. Remember containers can be tab or regular container.
Use them in a smart way and combine tabs and containers as required.

## Database Scripts

### When Updating DB Models

**ALWAYS provide TWO types of scripts:**

1. **ALTER Script** - For updating existing database:
```sql
ALTER TABLE Items
ADD IsDeleted BIT NOT NULL DEFAULT 0;

ALTER TABLE Items
ADD SortIndex INT NOT NULL DEFAULT 0;
```

2. **CREATE TABLE Script** - For updating DbCreation.sql file:
   - **If table is NEW**: Provide full CREATE TABLE script
   - **If table EXISTS**: Provide ONLY the new properties with indication of where to place them

**Example for NEW table:**
```sql
CREATE TABLE Items
(
    Id                      INT             IDENTITY(1,1),
    Name                    NVARCHAR(100)   NOT NULL,
    CategoryId              INT             NULL,
    Price                   DECIMAL(18,2)   NULL,
    
    -- Special properties at the end (before constraints)
    Active                  BIT             NOT NULL DEFAULT 1,
    CreateDate              DATETIME        NOT NULL DEFAULT GETDATE(),
    UpdateDate              DATETIME        NOT NULL DEFAULT GETDATE(),
    UpdateBy                INT             NOT NULL DEFAULT 1,
    SortIndex               INT             NOT NULL DEFAULT 0,
    IsDeleted               BIT             NOT NULL DEFAULT 0,
    
    -- Constraints (PK first, then FK, then uniques)
    CONSTRAINT PK_Items PRIMARY KEY(Id),
    CONSTRAINT FK_Items_Category FOREIGN KEY(CategoryId) REFERENCES Categories(Id),
    CONSTRAINT UQ_Items_Name UNIQUE(Name)
);

-- Indexes after table creation
CREATE NONCLUSTERED INDEX IX_Items_CategoryId ON Items (CategoryId);
CREATE NONCLUSTERED INDEX IX_Items_IsDeleted ON Items (IsDeleted);
```

**Example for EXISTING table (only new properties):**
```sql
-- Add these properties to Items table (after Price property, before constraints):
    SortIndex               INT             NOT NULL DEFAULT 0,
    IsDeleted               BIT             NOT NULL DEFAULT 0,

-- Add this index after table creation:
CREATE NONCLUSTERED INDEX IX_Items_IsDeleted ON Items (IsDeleted);
```

### Script Structure Rules

1. **CREATE TABLE first** - ID always first, special properties at the end
2. **Constraints after properties** - PK first, then FK, then uniques
3. **NULL/NOT NULL and DEFAULT** - On each property definition
4. **Indexes after table creation** - Use proper naming: `IX_TableName_ColumnName`
5. **Manual inserts** - Always with `SET IDENTITY_INSERT ON` before and `OFF` after
6. **Naming conventions** - Check existing scripts and follow the pattern:
   - Constraints: `PK_TableName`, `FK_TableName_ReferencedTable`, `UQ_TableName_Column`
   - Indexes: `IX_TableName_ColumnName`
7. **Enums** - Always `SMALLINT` with comment:
   ```sql
   RoleType        SMALLINT        NOT NULL,  -- 0 = DinoAdmin, 1 = RegularAdmin, 2 = Custom
   ```
8. **Comments** - Use comments on columns that might not be clear
9. **JSON data** - Always `NVARCHAR(MAX)`
10. **Database** - Always MSSQL (Microsoft SQL Server)

**IMPORTANT - When Updating Existing Tables:**
- If the table already exists, provide ONLY the new properties in the CREATE TABLE script
- Indicate WHERE to place them (e.g., "Add after Name property" or "Add before constraints")
- Example for existing table:
```sql
-- Add these properties to Items table (after Name property, before constraints):
    IsDeleted               BIT             NOT NULL DEFAULT 0,
    SortIndex               INT             NOT NULL DEFAULT 0,
```
Do NOT include existing properties in the CREATE TABLE script when updating existing tables.

### Special Properties in Scripts

When adding special properties, ensure they match framework requirements:
- `Archived` - BIT NOT NULL DEFAULT 0
- `IsDeleted` - BIT NOT NULL DEFAULT 0
- `Active` - BIT NOT NULL DEFAULT 1
- `CreateDate` - DATETIME NOT NULL DEFAULT GETDATE()
- `UpdateDate` - DATETIME NOT NULL DEFAULT GETDATE()
- `UpdateBy` - INT NOT NULL DEFAULT 1
- `SortIndex` - INT NOT NULL DEFAULT 0 (SUPER IMPORTANT for sorting)
- `LastLoginDate` - DATETIME NULL
- `LastIpAddress` - VARCHAR(50) NULL

**If property is missing:**
1. Add it to DB model
2. Add it to Admin model with proper attributes
3. Mention it clearly so it's noticed
4. Provide both ALTER and CREATE TABLE scripts

### DbContext Setup and Demo Cleanup

**CRITICAL**: When starting a new project, you MUST update the MainDbContext.cs:

1. **Remove Demo Entities:**
   ```csharp
   // REMOVE these from MainDbContext.cs:
   public DbSet<ItemCategory> ItemCategories { get; set; }
   public DbSet<Item> Items { get; set; }
   // ... all other demo DbSets
   ```

2. **Remove Demo Usings:**
   ```csharp
   // REMOVE this using:
   // using Vitrea.BL.Models.Demo;
   ```

3. **Add Project Entities:**
   ```csharp
   // ADD your project entities:
   public DbSet<YourEntity> YourEntities { get; set; }
   public DbSet<AnotherEntity> AnotherEntities { get; set; }
   ```

4. **Update OnModelCreating:**
   - Remove all demo table configurations
   - Add configurations for your project entities
   - Include proper relationships, indexes, and constraints

5. **Delete Demo Folders:**
   ```bash
   # Remove these folders completely:
   rm -rf Vitrea.BL/Demo/
   rm -rf Vitrea.BL/Models/Demo/
   ```

**Example of Clean MainDbContext.cs:**
```csharp
public class MainDbContext : BaseDbContext<MainDbContext>
{
    // Only keep Admin-related DbSets from base class
    // Add your project DbSets here:
    public DbSet<BreakerType> BreakerTypes { get; set; }
    public DbSet<BreakerActionType> BreakerActionTypes { get; set; }
    // ... etc

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add configurations for your entities here
        // Remove all demo configurations
    }
}
```

## Best Practices

1. **Always use attributes** - Don't try to manually configure UI
2. **Use ListSettings sparingly** - Only mark important fields for list view
3. **Leverage conditional visibility** - Use attributes or rules for dynamic UI
4. **Follow naming conventions** - Makes code more maintainable
5. **Use ComplexType/Repeater** - For nested objects, prefer attributes over manual handling
6. **Cache appropriately** - Use `[CacheModel]` on frequently accessed entities
7. **Use BL layer** - Don't put business logic in controllers
8. **Register after-save actions** - Use `RegisterAfterSuccessfulSaveAction` for side effects
9. **Delete Demo folders** - Remove demo content when starting new project
10. **Keep Basics folders** - Required admin user/role models must stay

## Common Pitfalls to Avoid

1. **Missing AdminFieldCommon** - Every property needs this attribute
2. **Missing field type attribute** - Every property needs a field type
3. **Incorrect attribute parameters** - Check attribute constructors carefully
4. **Forgetting ListSettings** - Properties won't appear in list without it
5. **Wrong inheritance** - Models must inherit `BaseAdminModel`
6. **Controller base class** - Must inherit `DinoAdminBaseEntityController<TModel, TEntity, TId>`
7. **Missing CreateAdminSegment** - Menu won't appear without it
8. **Missing CreateListDef** - List view won't work without it
9. **ID type mismatch** - Admin Model and Entity ID types must match
10. **Naming mismatch** - Entity, Model, Controller naming must follow conventions
11. **Forgetting to delete Demo folders** - Demo content should be removed
12. **Modifying Submodules** - Never modify code in Submodules folder
13. **Missing special properties** - If using archive/delete/sorting, ensure DB properties exist with exact naming
14. **Not providing both scripts** - Always provide ALTER and CREATE TABLE scripts when updating DB
15. **Wrong property types** - Enums must be SMALLINT, JSON must be NVARCHAR(MAX)
16. **Forgetting SortIndex** - Sorting is SUPER IMPORTANT, always include SortIndex for sortable lists
17. **Including Archive by default** - Archive is NOT included by default in new models
18. **Cannot upload files or pictures** - Make sure the variable type in the entity's admin model is FileContainerCollection

## API implementation
### API Implementation Guidelines
1. **Controllers Location**  
   - API endpoints are always implemented in controller classes located in the `/Controllers/` folder of the API project (e.g., `ProjectName.Api/Controllers/`).
   - Each logical feature or entity should have its own controller class.

2. **API Models Structure**  
   - API request and response models go in the `/Models/` folder within the API project (`ProjectName.Api/Models/`).
   - All API request models should be grouped in `RequestModels.cs`.
   - All API response models should be grouped in `ResponseModels.cs`.
   - Keep all requests in one file and all responses in another, unless the project grows significantly.
   - **Never** place model classes directly in controller files.

3. **Standard JSON Response Structure**  
   - API responses must use the helper:  
     ```csharp
     protected JsonResult CreateJsonResponse(bool result, dynamic data, string error, bool allowGet = true)
     ```
   - This creates a standard JSON object for all responses, formatted as:  
     ```csharp
     var jsonData = new
     {
         Result = result,
         Error = error,
         Data = data
     };
     ```
   - Example usage in a controller action:  
     ```csharp
     [HttpPost]
     public async Task<JsonResult> MyAction([FromBody] MyRequestModel request)
     {
         // ... business logic ...
         return CreateJsonResponse(true, myData, null);
     }
     ```

**Summary:**  
- Controllers = always in `/Controllers/`
- Models = always in `/Models/`, grouped in `RequestModels.cs` and `ResponseModels.cs`
- All API responses use `CreateJsonResponse`, which wraps the result, error message, and response data in a standard structure.


