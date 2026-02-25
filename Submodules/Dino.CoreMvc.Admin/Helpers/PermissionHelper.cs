using Dino.CoreMvc.Admin.Attributes.Permissions;
using System.Linq;

namespace Dino.CoreMvc.Admin.Helpers
{
    public static class PermissionHelper
    {
        /// <summary>
        /// Checks if a user has a specific permission based on the AdminPermissionAttribute,
        /// their role identifier, and their role type.
        /// Logic: If attribute is null, grant. Otherwise, check role criteria.
        /// If role matches, check FullAccess. If FullAccess is true, grant.
        /// If FullAccess is false, grant only if the specific permission flag (e.g., CanAdd) is true.
        /// </summary>
        /// <param name="attribute">The AdminPermissionAttribute applied to the controller or settings class. Can be null.</param>
        /// <param name="userRoleIdentifier">The user's role identifier (AdminRoleBase.Id). Pass -1 if not available or not authenticated.</param>
        /// <param name="userRoleType">The user's role type (AdminRoleBase.RoleType). Pass -1 if not available or not authenticated.</param>
        /// <param name="permissionToAssert">The specific permission being checked (e.g., PermissionType.View).</param>
        /// <returns>True if permission is granted, false otherwise.</returns>
        public static bool CheckPermission(AdminPermissionAttribute attribute, int userRoleIdentifier, short userRoleType, PermissionType permissionToAssert)
        {
            if (attribute == null)
            {
                return true;
            }

            bool userMatchesRoleCriteria = false;
            if (attribute.AllowedRoleIdentifiers != null && attribute.AllowedRoleIdentifiers.Any())
            {
                if (userRoleIdentifier != -1 && attribute.AllowedRoleIdentifiers.Contains(userRoleIdentifier))
                {
                    userMatchesRoleCriteria = true;
                }
            }
            else if (attribute.MinimumRoleTypeRequired != -1)
            {
                if (userRoleType != -1 && userRoleType <= attribute.MinimumRoleTypeRequired)
                {
                    userMatchesRoleCriteria = true;
                }
            }

            if (!userMatchesRoleCriteria)
            {
                return false;
            }

            // Role criteria met.
            // If FullAccess is true, grant permission immediately.
            bool allowed = attribute.FullAccess;

            // FullAccess is false, so check the specific permission flag.
            switch (permissionToAssert)
            {
                case PermissionType.View:
                    return attribute.CanView ?? allowed;
                case PermissionType.Add:
                    return attribute.CanAdd ?? allowed;
                case PermissionType.Edit:
                    return attribute.CanEdit ?? allowed;
                case PermissionType.Delete:
                    return attribute.CanDelete ?? allowed;
                case PermissionType.Archive:
                    return attribute.CanArchive ?? allowed;
                case PermissionType.Export:
                    return attribute.CanExport ?? allowed;
                case PermissionType.Import:
                    return attribute.CanImport ?? allowed;
                default:
                    return allowed;
            }
        }
    }
}