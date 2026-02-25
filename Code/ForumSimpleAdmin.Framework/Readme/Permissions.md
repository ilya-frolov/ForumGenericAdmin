# Admin Permission System

This document explains how to use the permission system for admin controllers and settings classes within the Dino.CoreMvc.Admin framework.

## Overview

The permission system allows fine-grained control over what admin users can see and do. It relies on the `AdminPermissionAttribute` applied to controller or settings classes.

- **Default Behavior**: If a controller or settings class has **no** `AdminPermissionAttribute`, it is considered fully accessible to any authenticated admin user (primarily affecting visibility in the admin home panel).
- **Attribute Present**: If the `AdminPermissionAttribute` **is** present, access is **denied by default** unless explicitly granted by the attribute's properties matching the user's role and the required permission flag evaluation (see "Important Rule" below).

Key user claims involved:
- `UserRoleIdentifier`: An `int` representing the user's `AdminRoleBase.Id`.
- `UserRoleType`: A `short` representing the user's `AdminRoleBase.RoleType` (e.g., 0 for DinoAdmin, 1 for RegularAdmin).

## `AdminPermissionAttribute`

Located in `Dino.CoreMvc.Admin.Attributes.Permissions.AdminPermissionAttribute`.

### Key Properties:

- **`AllowedRoleIdentifiers` (`int[]`)**: An array of `AdminRoleBase.Id` values. If the user's `UserRoleIdentifier` matches any ID in this array, the role criteria part of the permission check passes.
  ```csharp
  // Example: Only users with Role ID 1 or 3 can potentially access.
  [AdminPermission(AllowedRoleIdentifiers = new[] { 1, 3 }, CanView = true)]
  ```
- **`MinimumRoleTypeRequired` (`short`)**: The minimum `RoleType` required. Uses the `RoleType` enum (e.g., `(short)RoleType.RegularAdmin`). Smaller numeric values indicate higher privilege (e.g., DinoAdmin (0) > RegularAdmin (1)). If `AllowedRoleIdentifiers` is set, this property is typically ignored for role matching. If `AllowedRoleIdentifiers` is *not* set, this property is checked. If the user's `UserRoleType` is less than or equal to this value, the role criteria part passes.
  ```csharp
  // Example: Only users with RoleType of Admin (or higher, like DinoAdmin) can potentially access.
  [AdminPermission(MinimumRoleTypeRequired = (short)RoleType.Admin, CanView = true)]
  ```
- **`FullAccess` (`bool`)**: Defaults to `false`. If `true`, it acts as the default permission value for any specific `Can...` flag that is *not explicitly set* (i.e., left as `null`).
- **Permission Flags (`bool?`)**: **Important:** These flags must be defined as nullable booleans (`bool?`) in the `AdminPermissionAttribute.cs` file for this logic to work. They determine what actions are allowed if the role criteria (above) are met.
    - If a specific flag (e.g., `CanSave`) is **explicitly set** to `true` or `false`, that value takes precedence and determines the permission for that action.
    - If a specific flag is **not set** (remains `null`), its effective value is determined by the `FullAccess` flag.
    - **Flags:**
        - `CanView`: Allows viewing the item (e.g., in lists, menus).
        - `CanSave`: Allows creating new records or updating existing ones.
        - `CanEdit`: Allows updating existing records (more granular than `CanSave` if needed).
        - `CanDelete`: Allows deleting records.
        - `CanArchive`: Allows archiving records.
        - `CanExport`: Allows exporting data.
        - `CanImport`: Allows importing data.

### Constructors and Usage Examples:

1.  **Default Constructor (setting properties explicitly)**:
    ```csharp
    // Allows users with Role ID 1 or Role ID 2 to view and edit ONLY.
    // Other permissions inherit from FullAccess (default false), so they are denied.
    [AdminPermission(AllowedRoleIdentifiers = new[] { 1, 2 }, CanView = true, CanEdit = true)]
    public class MySecureController : DinoAdminBaseEntityController<...>
    { ... }

    // Allows users with RoleType Admin or higher (e.g., DinoAdmin) to have full access,
    // EXCEPT they explicitly cannot delete.
    [AdminPermission(MinimumRoleTypeRequired = (short)RoleType.Admin, FullAccess = true, CanDelete = false)]
    public class CriticalSettings : AdminBaseSettings
    { ... }

    // Allows users with Role ID 5 full access. Specific flags are null, so they inherit from FullAccess = true.
    [AdminPermission(AllowedRoleIdentifiers = new[] { 5 }, FullAccess = true)]
    public class Role5FullAccessController : DinoAdminBaseEntityController<...>
    { ... }\
    ```

2.  **Constructor for `AllowedRoleIdentifiers`**:
    You can pass role identifiers directly to the constructor. Permission flags are then set as properties.
    ```csharp
    // Allows users with Role ID 1 or 5 to have full access.
    [AdminPermission(1, 5, FullAccess = true)]
    public class AnotherController : DinoAdminBaseEntityController<...>
    { ... }

    // Allows users with Role ID 10 ONLY to view. Other permissions inherit from FullAccess (default false).
    [AdminPermission(new[]{ 10 }, CanView = true)]
    public class SpecificViewController : DinoAdminBaseEntityController<...>
    { ... }
    ```

3.  **Constructor for `MinimumRoleTypeRequired`**:
    You can pass the minimum role type directly to the constructor. Permission flags are then set as properties.
    ```csharp
    // Allows users with RoleType Editor or higher ONLY to view and edit.
    [AdminPermission((short)RoleType.Editor, CanView = true, CanEdit = true)]
    public class EditorAccessibleSettings : AdminBaseSettings
    { ... }
    ```

### Important Rule (Evaluation Logic):

If an `AdminPermissionAttribute` is applied to a class:
1. The user's role must match **either** the `AllowedRoleIdentifiers` **or** the `MinimumRoleTypeRequired` criteria. If not, permission is denied.
   - (`AllowedRoleIdentifiers` takes precedence if set).
2. If the role criteria are met, the specific permission being checked (e.g., `PermissionType.Edit`) is evaluated:
   - Look at the corresponding flag on the attribute (e.g., `CanEdit`).
   - If the flag has been **explicitly set** (is not `null`), its value (`true` or `false`) determines the outcome. **This overrides `FullAccess`**.
   - If the flag has **not been set** (is `null`), the value of the `FullAccess` flag determines the outcome.

### Example Scenarios:

- `[AdminPermission(FullAccess = true, CanDelete = false)]` applied to Role X:
    - User with Role X tries to delete: `CanDelete` is set to `false`, permission denied.
    - User with Role X tries to save: `CanSave` is `null`, permission granted via `FullAccess = true`.
- `[AdminPermission(CanView = true)]` applied to Role Y:
    - User with Role Y tries to view: `CanView` is set to `true`, permission granted.
    - User with Role Y tries to edit: `CanEdit` is `null`, permission denied via `FullAccess` (default `false`).


## How Permissions Affect Admin Home

The `DinoAdminBaseHomeController` uses this permission system to determine which controllers (segments) and settings appear in the admin navigation.
- It checks for the `AdminPermissionAttribute` on each discovered controller and settings class.
- It then uses `PermissionHelper.CheckPermission` with `PermissionType.View` to see if the current user has view access based on the logic described above.
- If permission is denied, the item will not be displayed in the admin home panel.

## Programmatic Permission Checks (Advanced)

While most permission enforcement is automatic (via `PermissionHelper` in base controllers and home controller), you can perform checks manually.

```csharp
using Dino.CoreMvc.Admin.Attributes.Permissions;
using Dino.CoreMvc.Admin.Helpers;

// ... inside a controller method or service ...
var permissionAttribute = typeof(MyControllerOrSettingsClass).GetCustomAttribute<AdminPermissionAttribute>();
int currentUserRoleIdentifier = GetCurrentUserRoleIdentifier(); // From DinoAdminBaseController
short currentUserRoleType = GetCurrentUserRoleType(); // From DinoAdminBaseController

// Use the helper for consistent evaluation:
bool canEdit = PermissionHelper.CheckPermission(permissionAttribute, currentUserRoleIdentifier, currentUserRoleType, PermissionType.Edit);

// OR if using the controller's helper (which calls the static helper):
// Assuming you have a CheckPermission method in your controller that uses PermissionHelper
// bool canEdit = await CheckPermission(PermissionType.Edit); // In methods within DinoAdminBaseEntityController derivatives

if (canEdit)
{
    // Proceed with edit logic
}
else
{
    // Deny access or return an error
}
```

The `PermissionType` enum is located in `Dino.CoreMvc.Admin.Attributes.Permissions.PermissionType`.

This centralized helper ensures consistent permission evaluation across the application.