using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dino.Mvc.Common.Security
{
    public class AllowCrossSiteJsonAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var context = filterContext.HttpContext;

            string origin = "*";
            var header = context.Request.GetTypedHeaders();
            if (header.Referer != null)
            {
                origin = header.Referer.GetLeftPart(UriPartial.Authority);
            }

            context.Response.Headers.Add("Access-Control-Allow-Origin", origin);
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST");
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
            context.Response.Headers.Add("Access-Control-Max-Age", "1728000");

            if (context.Request.Method.Equals("OPTIONS", StringComparison.InvariantCultureIgnoreCase))
            {
	            filterContext.Result = new EmptyResult();
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
