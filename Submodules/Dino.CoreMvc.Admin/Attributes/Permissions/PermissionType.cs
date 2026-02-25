namespace Dino.CoreMvc.Admin.Attributes.Permissions
{
    public enum PermissionType
    {
        View,
        Add, // Covers Create and Update
        Edit, // More granular Update if needed, otherwise Add can be used
        Delete,
        Archive,
        Export,
        Import
    }
} 