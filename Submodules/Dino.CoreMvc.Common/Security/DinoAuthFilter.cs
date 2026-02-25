using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dino.Mvc.Common.Security
{
	public class DinoAuthFilter : ActionFilterAttribute, IAuthorizationFilter
	{
		public DinoAuthFilter()
		{

		}

		public void OnAuthorization(AuthorizationFilterContext context)
		{
			var isAuthenticated = false;
			var isAuthorized = false;

			if (context.HttpContext.User.Identity.IsAuthenticated)
			{
				isAuthenticated = true;
			}

			// Checks if the AllowAnonymous attribute is defined in either the action or the controller
			var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
			if ((controllerActionDescriptor != null) && 
			    controllerActionDescriptor.MethodInfo.GetCustomAttributes(inherit: true).Any(a => a.GetType() == typeof(AllowAnonymousAttribute)))
			{
				isAuthorized = true;
			}
			// Checks if the user is logged in.
			else if (isAuthenticated)
			{
				isAuthorized = true;
			}

			if (!isAuthorized)
			{
				context.Result = new JsonResult(new
				{
					Error = "Unauthorized"
				});
			}
		}
	}
}
