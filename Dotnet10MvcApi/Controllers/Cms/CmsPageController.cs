using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dotnet10MvcApi.Services.Cms;

namespace Dotnet10MvcApi.Controllers.Cms
{
    [Route("cms")]
    public class CmsPageController : Controller
    {
        private readonly CmsService _cmsService;

        public CmsPageController(CmsService cmsService)
        {
            _cmsService = cmsService;
        }

        // GET /cms/{slug}
        [HttpGet("{slug}")]
        public async Task<IActionResult> Page(string slug)
        {
            var page = await _cmsService.GetPageBySlugAsync(slug);
            if (page == null)
            {
                return NotFound();
            }
            return View("~/Views/Cms/Page.cshtml", page);
        }
    }
}
