using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Dino.Mvc.Common.ModelBinders
{
	public class NullableDateTimeModelBinderProvider : IModelBinderProvider
	{
		private string[] _customFormat;

		public NullableDateTimeModelBinderProvider(string[] customFormat)
		{
			_customFormat = customFormat;
		}

		public NullableDateTimeModelBinderProvider(string customFormat)
		{
			_customFormat = new string[] { customFormat };
		}

		public IModelBinder GetBinder(ModelBinderProviderContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException(nameof(context));
			}

			if (context.Metadata.ModelType == typeof(DateTime?))
			{
				return new NullableDateTimeModelBinder(_customFormat);
			}

			return null;
		}
	}
}
