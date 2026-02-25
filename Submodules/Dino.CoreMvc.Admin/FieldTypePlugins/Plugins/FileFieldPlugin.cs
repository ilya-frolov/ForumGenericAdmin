using System;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.FieldTypePlugins.Plugins.Base;

namespace Dino.CoreMvc.Admin.FieldTypePlugins.Plugins
{

    /// <summary>
    /// Plugin for handling file fields with platform-specific file collections.
    /// </summary>
    public class FileFieldPlugin : FileFieldBasePlugin<AdminFieldFileAttribute>
    {
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "File";

        public FileFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
} 