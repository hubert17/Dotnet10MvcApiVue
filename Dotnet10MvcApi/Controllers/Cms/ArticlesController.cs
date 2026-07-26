using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dotnet10MvcApi.Services.Cms;

namespace Dotnet10MvcApi.Controllers.Cms
{
    [Route("articles")]
    public class ArticlesController : Controller
    {
        private readonly CmsService _cmsService;

        public ArticlesController(CmsService cmsService)
        {
            _cmsService = cmsService;
        }

        // GET /articles
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var articles = await _cmsService.GetArticlesAsync();
            return View("~/Views/Cms/ArticleList.cshtml", articles);
        }

        // GET /articles/{slug}
        [HttpGet("{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            var article = await _cmsService.GetArticleBySlugAsync(slug);
            if (article == null)
            {
                return NotFound();
            }
            return View("~/Views/Cms/ArticlePost.cshtml", article);
        }
    }
}
