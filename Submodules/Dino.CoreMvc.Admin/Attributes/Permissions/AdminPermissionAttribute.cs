using System;

namespace Dino.CoreMvc.Admin.Attributes.Permissions
{
    /// <summary>
    /// Specifies the permissions required to access an admin controller or setting.
    /// Permissions can be granted based on specific role identifiers or a minimum role type.
    /// If this attribute is present, access is denied by default unless explicitly granted by matching role criteria and corresponding permission flags.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class AdminPermissionAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets an array of Role Identifiers (AdminRoleBase.Id) that are allowed access.
        /// </summary>
        public int[] AllowedRoleIdentifiers { get; set; }

        /// <summary>
        /// Gets or sets the minimum RoleType (e.g., (short)RoleType.Admin) required for access.
        /// Smaller values indicate higher privilege (e.g., 0 > 1 > 2).
        /// Defaults to -1, indicating it's not set and this criterion won't be used for matching.
        /// </summary>
        public short MinimumRoleTypeRequired { get; set; } = -1;

        /// <summary>
        /// Grants full access (view, save, edit, delete, archive, export) if true and role criteria are met. Overrides individual permissions.
        /// Defaults to false.
        /// </summary>
        public bool FullAccess { get; set; } = true;

        /// <summary>
        /// Grants permission to view if true and role criteria are met.
        /// Defaults to false.
        /// </summary>
        public bool? CanView { get; set; }

        /// <summary>
        /// Grants permission to save (create or update) if true and role criteria are met.
        /// Defaults to false.
        /// </summary>
        public bool? CanAdd { get; set; }

        /// <summary>
        /// Grants permission to edit (update existing) if true and role criteria are met.
        /// Defaults to false.
        /// </summary>
        public bool? CanEdit { get; set; }

        /// <summary>
        /// Grants permission to delete if true and role criteria are met.
        /// Defaults to false.
        /// </summary>
        public bool? CanDelete { get; set; }

        /// <summary>
        /// Grants permission to archive if true and role criteria are met.
        /// Defaults to false.
        /// </summary>
        public bool? CanArchive { get; set; }

        /// <summary>
        /// Grants permission to export data if true and role criteria are met.
        /// Defaults to false.
        /// </summary>
        public bool? CanExport { get; set; }

        /// <summary>
        /// Grants permission to import data if true and role criteria are met.
        /// Defaults to false.
        /// </summary>
        public bool? CanImport { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminPermissionAttribute"/> class.
        /// Use this constructor to set role criteria and permissions via properties, for example:
        /// [AdminPermission(AllowedRoleIdentifiers = new[] {1, 2}, CanView = true, CanEdit = true)]
        /// [AdminPermission(MinimumRoleTypeRequired = 0, FullAccess = true)]
        /// [AdminPermission(CanView = true)] // If no role identifiers/type, applies to any authenticated user if other checks pass (not typical for role-based).
        /// </summary>
        public AdminPermissionAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminPermissionAttribute"/> class, targeting specific role identifiers.
        /// Individual permissions (CanView, CanEdit, etc.) or FullAccess must be set separately via properties.
        /// Example: [AdminPermission(1, 2, FullAccess = true)] or [AdminPermission(new[]{1,2}, CanView = true)]
        /// </summary>
        /// <param name="allowedRoleIdentifiers">The identifiers of roles that are allowed.</param>
        public AdminPermissionAttribute(params int[] allowedRoleIdentifiers)
        {
            AllowedRoleIdentifiers = allowedRoleIdentifiers;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminPermissionAttribute"/> class, targeting a minimum role type.
        /// Individual permissions (CanView, CanEdit, etc.) or FullAccess must be set separately via properties.
        /// Example: [AdminPermission(RoleType.Admin, CanView = true)]
        /// </summary>
        /// <param name="minimumRoleTypeRequired">The minimum role type required (e.g., (short)RoleType.Admin).</param>
        public AdminPermissionAttribute(short minimumRoleTypeRequired)
        {
            MinimumRoleTypeRequired = minimumRoleTypeRequired;
        }
    }
} 