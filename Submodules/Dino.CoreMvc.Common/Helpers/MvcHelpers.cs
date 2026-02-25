using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Dino.CoreMvc.Common.Helpers
{
	public static class MvcHelpers
	{
		public static bool HasAttribute<T>(this ActionDescriptor actionDescriptor) where T : Attribute
		{
			var hasAttribute = false;

			if (actionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
			{
				// Check if the attribute exists on the action method
				if (controllerActionDescriptor.MethodInfo?.GetCustomAttribute<T>(inherit: true) != null)
				{
					hasAttribute = true;
				}

				// Check if the attribute exists on the controller
				if (controllerActionDescriptor.ControllerTypeInfo?.GetCustomAttribute<T>(inherit: true) != null)
				{
					hasAttribute = true;
				}
			}

			return hasAttribute;
		}
	}
}
