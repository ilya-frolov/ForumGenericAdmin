using System.Collections.Generic;
using System.Reflection;
using Dino.CoreMvc.Admin.Attributes;
using Dino.CoreMvc.Admin.FieldTypePlugins.Plugins.Base;

namespace Dino.CoreMvc.Admin.FieldTypePlugins
{
    /// <summary>
    /// Plugin for URL field types
    /// </summary>
    public class URLFieldPlugin : TextInputsBasePlugin<AdminFieldURLAttribute>
    {
        /// <summary>
        /// Gets the field type this plugin handles
        /// </summary>
        public override string FieldType => "URL";

        public URLFieldPlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
} 