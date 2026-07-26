using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dotnet10MvcApi.Models.Cms;
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

            var comments = await _cmsService.GetPostCommentsAsync(post.Id, onlyApproved: true);
            ViewBag.CommentsModel = new CommentSectionViewModel
            {
                PostId = post.Id,
                EnableComments = post.EnableComments,
                Comments = comments,
                FormAction = $"/blogs/{slug}/comment"
            };

            return View("~/Views/Cms/BlogPost.cshtml", post);
        }

        // POST /blogs/{slug}/comment
        [HttpPost("{slug}/comment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveComment(string slug, [FromForm] string authorName, [FromForm] string authorEmail, [FromForm] string commentBody, [FromForm] string? authorUrl)
        {
            var post = await _cmsService.GetBlogPostBySlugAsync(slug);
            if (post == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(authorName) || string.IsNullOrWhiteSpace(authorEmail) || string.IsNullOrWhiteSpace(commentBody))
            {
                TempData["CommentError"] = "Please fill in all required fields (Name, Email, and Comment).";
                return Redirect($"/blogs/{slug}#comments");
            }

            var savedComment = await _cmsService.SavePostCommentAsync(post.Id, authorName.Trim(), authorEmail.Trim(), commentBody.Trim(), authorUrl?.Trim());
            if (savedComment != null)
            {
                if (savedComment.IsApproved)
                {
                    TempData["CommentSuccess"] = "Your comment has been published successfully.";
                }
                else
                {
                    TempData["CommentSuccess"] = "Your comment has been submitted and is queued for moderation.";
                }
            }
            else
            {
                TempData["CommentError"] = "An error occurred while submitting your comment. Please try again.";
            }

            return Redirect($"/blogs/{slug}#comments");
        }
    }
}
