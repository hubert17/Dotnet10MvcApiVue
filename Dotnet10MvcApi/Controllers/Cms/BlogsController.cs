using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dotnet10MvcApi.Services.Cms;

namespace Dotnet10MvcApi.Controllers.Cms
{
    [Route("blogs")]
    public class BlogsController : Controller
    {
        private readonly CmsService _cmsService;

        public BlogsController(CmsService cmsService)
        {
            _cmsService = cmsService;
        }

        // GET /blogs
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var posts = await _cmsService.GetBlogPostsAsync();
            return View("~/Views/Cms/BlogList.cshtml", posts);
        }

        // GET /blogs/{slug}
        [HttpGet("{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var post = await _cmsService.GetBlogPostBySlugAsync(slug);
            if (post == null)
            {
                return NotFound();
            }
            return View("~/Views/Cms/BlogPost.cshtml", post);
        }
    }
}
