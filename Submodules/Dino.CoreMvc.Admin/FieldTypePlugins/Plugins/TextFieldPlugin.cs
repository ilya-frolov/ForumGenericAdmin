using System.Collections.Generic;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.FieldTypePlugins.Plugins.Base;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Plugin for text field types
    /// </summary>
    public class TextFieldPlugin : TextInputsBasePlugin<AdminFieldTextAttribute>
    {
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "Text";

        public TextFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
} 