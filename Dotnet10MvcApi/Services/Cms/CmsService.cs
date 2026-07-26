using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Piranha;
using Piranha.Models;
using Dotnet10MvcApi.Models.Cms;

namespace Dotnet10MvcApi.Services.Cms
{
    public class CmsService
    {
        private readonly IApi _api;

        public CmsService(IApi api)
        {
            _api = api;
        }

        public async Task<IEnumerable<BlogPost>> GetBlogPostsAsync()
        {
            try
            {
                var pages = await _api.Pages.GetAllAsync();
                var blogPage = pages.FirstOrDefault(p => p.Slug.Equals("blogs", StringComparison.OrdinalIgnoreCase) || p.Slug.Equals("/blogs", StringComparison.OrdinalIgnoreCase));
                
                if (blogPage != null)
                {
                    var posts = await _api.Posts.GetAllAsync<BlogPost>(blogPage.Id);
                    if (posts != null)
                    {
                        return posts.OrderByDescending(p => p.Published);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching blog posts: {ex.Message}");
            }
            return Enumerable.Empty<BlogPost>();
        }

        public async Task<BlogPost?> GetBlogPostBySlugAsync(string slug)
        {
            try
            {
                return await _api.Posts.GetBySlugAsync<BlogPost>("blogs", slug);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching blog post '{slug}': {ex.Message}");
                return null;
            }
        }

        public async Task<IEnumerable<ArticlePost>> GetArticlesAsync()
        {
            try
            {
                var pages = await _api.Pages.GetAllAsync();
                var articlePage = pages.FirstOrDefault(p => p.Slug.Equals("articles", StringComparison.OrdinalIgnoreCase) || p.Slug.Equals("/articles", StringComparison.OrdinalIgnoreCase));
                
                if (articlePage != null)
                {
                    var articles = await _api.Posts.GetAllAsync<ArticlePost>(articlePage.Id);
                    if (articles != null)
                    {
                        return articles.OrderByDescending(p => p.Published);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching articles: {ex.Message}");
            }
            return Enumerable.Empty<ArticlePost>();
        }

        public async Task<ArticlePost?> GetArticleBySlugAsync(string slug)
        {
            try
            {
                return await _api.Posts.GetBySlugAsync<ArticlePost>("articles", slug);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching article '{slug}': {ex.Message}");
                return null;
            }
        }

        public async Task<StandardPage?> GetPageBySlugAsync(string slug)
        {
            try
            {
                return await _api.Pages.GetBySlugAsync<StandardPage>(slug);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching CMS page '{slug}': {ex.Message}");
                return null;
            }
        }

        public async Task<IEnumerable<Comment>> GetPostCommentsAsync(Guid postId, bool onlyApproved = true)
        {
            try
            {
                return await _api.Posts.GetAllCommentsAsync(postId, onlyApproved);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching comments for post '{postId}': {ex.Message}");
                return Enumerable.Empty<Comment>();
            }
        }

        public async Task<bool> SavePostCommentAsync(Guid postId, string author, string email, string body, string? url = null)
        {
            try
            {
                var comment = new PostComment
                {
                    Author = author,
                    Email = email,
                    Body = body,
                    Url = url,
                    Created = DateTime.Now
                };
                await _api.Posts.SaveCommentAsync(postId, comment);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving comment for post '{postId}': {ex.Message}");
                return false;
            }
        }
    }
}
