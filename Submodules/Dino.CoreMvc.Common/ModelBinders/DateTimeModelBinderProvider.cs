using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Dino.Mvc.Common.ModelBinders
{
	public class DateTimeModelBinderProvider : IModelBinderProvider
	{
		private string[] _customFormat;

		public DateTimeModelBinderProvider(string[] customFormat)
		{
			_customFormat = customFormat;
		}

		public DateTimeModelBinderProvider(string customFormat)
		{
			_customFormat = new string[] { customFormat };
		}

		public IModelBinder GetBinder(ModelBinderProviderContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException(nameof(context));
			}

			if (context.Metadata.ModelType == typeof(DateTime))
			{
				return new DateTimeModelBinder(_customFormat);
			}

			return null;
		}
	}
}
