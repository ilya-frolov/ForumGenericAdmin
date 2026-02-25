using ForumSimpleAdmin.Api.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace ForumSimpleAdmin.Api.Controllers
{
    public class HomeController : MainAppBaseController<HomeController>
    {
        public IActionResult Index()
        {
            ContentResult result = new ContentResult
            {
                ContentType = "text/html",
                Content = "<html><body><h2>ForumSimpleAdmin API is running</h2></body></html>"
            };

            return result;
        }
    }
}
