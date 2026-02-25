using Dino.Common.Helpers;
using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Dino.Mvc.Common.ModelBinders
{
	public class NullableDateTimeModelBinder : IModelBinder
	{
		private readonly string[] _customFormat;

		public NullableDateTimeModelBinder(string[] customFormat)
		{
			_customFormat = customFormat;
		}

	    public NullableDateTimeModelBinder(string customFormat)
	    {
	        _customFormat = new string[] { customFormat };
        }

	    public Task BindModelAsync(ModelBindingContext bindingContext)
	    {
		    if (bindingContext == null)
		    {
			    throw new ArgumentNullException(nameof(bindingContext));
		    }

		    var modelName = bindingContext.ModelName;

		    // Try to get the value with the exact name
		    var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

		    if (valueProviderResult == ValueProviderResult.None)
		    {
			    // Try to get the value with the camel case name
			    var nameFixer = new StringBuilder(modelName);
			    nameFixer[0] = Char.ToLower(nameFixer[0]);
			    var camelModelName = nameFixer.ToString();

			    valueProviderResult = bindingContext.ValueProvider.GetValue(camelModelName);
			    if (valueProviderResult == ValueProviderResult.None)
			    {
				    return Task.CompletedTask;
			    }
		    }

		    bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

		    var rawValue = valueProviderResult.FirstValue;

		    if (rawValue.IsNotNullOrEmpty())
		    {
				bindingContext.Result = ModelBindingResult.Success(null);
				return Task.CompletedTask;
		    }

		    bool success = DateTime.TryParseExact(rawValue, _customFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value);
		    if (!success)
		    {
			    bindingContext.ModelState.TryAddModelError(modelName, "DateTime format is invalid.");
			    return Task.CompletedTask;
		    }

		    bindingContext.Result = ModelBindingResult.Success(value);
		    return Task.CompletedTask;
	    }
	}
}
