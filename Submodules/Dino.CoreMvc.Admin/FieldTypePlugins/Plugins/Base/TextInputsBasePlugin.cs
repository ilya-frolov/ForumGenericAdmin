using Dino.CoreMvc.Admin.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dino.CoreMvc.Admin.FieldTypePlugins.Plugins.Base
{
    public abstract class TextInputsBasePlugin<TAttribute> : BaseFieldTypePlugin<TAttribute, string>
        where TAttribute : AdminFieldBaseAttribute
    {
        public TextInputsBasePlugin(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Validates a text field value
        /// </summary>
        public override (bool IsValid, List<string> ErrorMessages) Validate(string value, PropertyInfo property)
        {
            // Get the field attribute
            var fieldAttribute = property.GetCustomAttribute<AdminFieldTextAttribute>();

            // First validate using base implementation (handles required fields)
            var baseResult = base.Validate(value, property);
            if (!baseResult.IsValid)
                return baseResult;

            // No further validation if value is null or empty
            if (string.IsNullOrEmpty(value))
                return (true, new List<string>());

            var errorMessages = new List<string>();

            // Check maximum length
            if (fieldAttribute.MaxLength != null && value.Length > fieldAttribute.MaxLength)
            {
                errorMessages.Add($"Field '{property.Name}' exceeds maximum length of {fieldAttribute.MaxLength} characters");
            }

            return (errorMessages.Count == 0, errorMessages);
        }
    }
}
