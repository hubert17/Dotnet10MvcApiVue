using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dotnet10MvcApi.Data;
using Dotnet10MvcApi.Models.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Piranha;

namespace Dotnet10MvcApi.Services.Cms
{
    /// <summary>
    /// Security Guard Middleware for Piranha CMS Post Management.
    /// Allows CmsWriter users to view, edit, and save drafts of others' posts, but restricts PUBLISHING to ONLY their own posts and PREVENT duplicate creation attempts.
    /// </summary>
    public class PiranhaPostSecurityMiddleware
    {
        private readonly RequestDelegate _next;

        public PiranhaPostSecurityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/manager/api/post/save") &&
                HttpMethods.IsPost(context.Request.Method) &&
                context.User.Identity?.IsAuthenticated == true)
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                var bodyText = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(bodyText))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(bodyText);
                        var root = doc.RootElement;

                        var pathValue = context.Request.Path.Value ?? "";

                        // Primary URL Path Authority Check:
                        var isDraftAction = pathValue.EndsWith("/save/draft", StringComparison.OrdinalIgnoreCase) ||
                                           pathValue.EndsWith("/save/unpublish", StringComparison.OrdinalIgnoreCase);

                        var isPublishAction = pathValue.EndsWith("/save/publish", StringComparison.OrdinalIgnoreCase);

                        // Secondary JSON Payload Fallback (for generic /manager/api/post/save requests):
                        if (!isDraftAction && !isPublishAction)
                        {
                            if (root.TryGetProperty("action", out var actProp))
                            {
                                var actStr = actProp.GetString() ?? "";
                                if (string.Equals(actStr, "draft", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(actStr, "unpublish", StringComparison.OrdinalIgnoreCase))
                                {
                                    isDraftAction = true;
                                }
                                else if (string.Equals(actStr, "publish", StringComparison.OrdinalIgnoreCase))
                                {
                                    isPublishAction = true;
                                }
                            }

                            if (!isDraftAction && root.TryGetProperty("state", out var stateProp))
                            {
                                var stateStr = stateProp.GetString() ?? "";
                                if (string.Equals(stateStr, "draft", StringComparison.OrdinalIgnoreCase))
                                {
                                    isDraftAction = true;
                                }
                                else if (string.Equals(stateStr, "published", StringComparison.OrdinalIgnoreCase))
                                {
                                    isPublishAction = true;
                                }
                            }

                            if (!isDraftAction && root.TryGetProperty("status", out var statusProp))
                            {
                                var statusStr = statusProp.GetString() ?? "";
                                if (string.Equals(statusStr, "draft", StringComparison.OrdinalIgnoreCase))
                                {
                                    isDraftAction = true;
                                }
                                else if (string.Equals(statusStr, "published", StringComparison.OrdinalIgnoreCase))
                                {
                                    isPublishAction = true;
                                }
                            }

                            // If not explicitly a draft save, check for non-empty published date string
                            if (!isDraftAction && root.TryGetProperty("published", out var pubProp) &&
                                pubProp.ValueKind == JsonValueKind.String &&
                                !string.IsNullOrWhiteSpace(pubProp.GetString()))
                            {
                                isPublishAction = true;
                            }
                        }

                        var currentUsername = context.User.Identity?.Name ?? "";
                        var isUserAdmin = context.User.IsInRole(UserAccount.DEFAULT_ADMIN_ROLENAME) ||
                                          context.User.IsInRole("admin") || context.User.IsInRole("Admin");
                        var isUserEditor = context.User.IsInRole("CmsEditor") || context.User.IsInRole("editor") || context.User.IsInRole("Editor");

                        // Extract properties from payload for comprehensive post lookup
                        var idStr = GetPropString(root, "id", "Id");
                        Guid.TryParse(idStr, out var postId);

                        var blogIdStr = GetPropString(root, "blogId", "BlogId");
                        Guid.TryParse(blogIdStr, out var blogId);

                        var slug = GetPropString(root, "slug", "Slug");
                        var title = GetPropString(root, "title", "Title");

                        var api = context.RequestServices.GetRequiredService<IApi>();
                        Piranha.Models.DynamicPost existingPost = null;

                        // 1. Lookup by ID
                        if (postId != Guid.Empty)
                        {
                            existingPost = await api.Posts.GetByIdAsync(postId);
                        }

                        // 2. Lookup by exact slug
                        if (existingPost == null && !string.IsNullOrWhiteSpace(slug))
                        {
                            if (blogId != Guid.Empty)
                            {
                                existingPost = await api.Posts.GetBySlugAsync(blogId, slug);
                            }
                            if (existingPost == null)
                            {
                                var site = await api.Sites.GetDefaultAsync();
                                if (site != null)
                                {
                                    var pages = await api.Pages.GetAllAsync(site.Id);
                                    foreach (var page in pages)
                                    {
                                        try
                                        {
                                            var found = await api.Posts.GetBySlugAsync(page.Id, slug);
                                            if (found != null) { existingPost = await api.Posts.GetByIdAsync(found.Id); break; }
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }

                        // 3. Lookup by base slug (stripping trailing -1, -2, -20260801, etc.)
                        if (existingPost == null && !string.IsNullOrWhiteSpace(slug))
                        {
                            var baseSlug = Regex.Replace(slug, @"(-\d+|-([a-f0-9]{4,}))$", "", RegexOptions.IgnoreCase);
                            if (!string.IsNullOrWhiteSpace(baseSlug) && baseSlug != slug)
                            {
                                if (blogId != Guid.Empty)
                                {
                                    existingPost = await api.Posts.GetBySlugAsync(blogId, baseSlug);
                                }
                                if (existingPost == null)
                                {
                                    var site = await api.Sites.GetDefaultAsync();
                                    if (site != null)
                                    {
                                        var pages = await api.Pages.GetAllAsync(site.Id);
                                        foreach (var page in pages)
                                        {
                                            try
                                            {
                                                var found = await api.Posts.GetBySlugAsync(page.Id, baseSlug);
                                                if (found != null) { existingPost = await api.Posts.GetByIdAsync(found.Id); break; }
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }
                        }

                        // 4. Lookup by exact Title across all site pages/blog archives
                        if (existingPost == null && !string.IsNullOrWhiteSpace(title))
                        {
                            var site = await api.Sites.GetDefaultAsync();
                            if (site != null)
                            {
                                var pages = await api.Pages.GetAllAsync(site.Id);
                                foreach (var page in pages)
                                {
                                    if (existingPost != null) break;
                                    try
                                    {
                                        var postsInPage = await api.Posts.GetAllAsync(page.Id);
                                        if (postsInPage != null)
                                        {
                                            var match = postsInPage.FirstOrDefault(p =>
                                                string.Equals(p.Title?.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase));
                                            if (match != null)
                                            {
                                                existingPost = await api.Posts.GetByIdAsync(match.Id);
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }

                        // Extract createdBy tag from existing post MetaKeywords or incoming JSON payload
                        string createdBy = "";
                        var metaKeywords = existingPost?.MetaKeywords ?? "";
                        if (string.IsNullOrWhiteSpace(metaKeywords) && root.TryGetProperty("metaKeywords", out var incomingMetaProp) && incomingMetaProp.ValueKind == JsonValueKind.String)
                        {
                            metaKeywords = incomingMetaProp.GetString() ?? "";
                        }

                        if (!string.IsNullOrWhiteSpace(metaKeywords))
                        {
                            foreach (var tag in metaKeywords.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                var trimmed = tag.Trim();
                                if (trimmed.StartsWith("createdby:", StringComparison.OrdinalIgnoreCase))
                                {
                                    createdBy = trimmed.Substring("createdby:".Length).Trim();
                                    break;
                                }
                                if (string.IsNullOrEmpty(createdBy) && trimmed.StartsWith("author:", StringComparison.OrdinalIgnoreCase))
                                {
                                    createdBy = trimmed.Substring("author:".Length).Trim();
                                }
                            }
                        }

                        // Fallback for existing posts where createdBy was not set: default creator to UserAccount.DEFAULT_ADMIN_LOGIN
                        if (existingPost != null && string.IsNullOrWhiteSpace(createdBy))
                        {
                            createdBy = UserAccount.DEFAULT_ADMIN_LOGIN;
                        }

                        // If post is genuinely NEW (no existing match in DB by ID, slug, or title), default createdBy to current user
                        if (existingPost == null && string.IsNullOrWhiteSpace(createdBy))
                        {
                            createdBy = currentUsername;
                        }

                        // If user is a CmsWriter (not Admin and not Editor), enforce ownership security rules
                        if (!isUserAdmin && !isUserEditor)
                        {
                            var isOwnPost = string.Equals(createdBy, currentUsername, StringComparison.OrdinalIgnoreCase);

                            // Check active creator status when publishing
                            if (isPublishAction)
                            {
                                var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                                var isCreatorActive = !string.IsNullOrWhiteSpace(createdBy) &&
                                    await db.Users.AnyAsync(u => u.UserName.ToLower() == createdBy.ToLower() && u.IsActive);

                                if (string.IsNullOrWhiteSpace(createdBy) || !isCreatorActive)
                                {
                                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync(BuildErrorJsonResponse("Publish Restricted", "Publishing is restricted. The creator of this post is inactive or missing. Please ask a CmsEditor or Admin to review and publish."));
                                    return;
                                }
                            }

                            // Rule A: Block creating new/duplicate posts derived from another author's post
                            if (existingPost != null && postId != existingPost.Id && !isOwnPost)
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync(BuildErrorJsonResponse("Error", "An error occurred. Your changes were not saved. Please refresh the browser and try again."));
                                return;
                            }

                            // Rule B: Block PUBLISHING (action == "publish") for another author's post
                            if (isPublishAction && !isOwnPost)
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync(BuildErrorJsonResponse("Publish Restricted", "Publishing is restricted. Please save your edits and ask an Editor or Admin to review and publish it."));
                                return;
                            }
                        }
                    }
                    catch
                    {
                        // Fallthrough on non-JSON payloads
                    }
                }
            }

            await _next(context);
        }

        private static string BuildErrorJsonResponse(string title, string message)
        {
            var statusObj = new
            {
                status = new
                {
                    type = "danger",
                    title = title,
                    body = message,
                    message = message
                }
            };
            return JsonSerializer.Serialize(statusObj);
        }

        private static string GetPropString(JsonElement elem, params string[] propNames)
        {
            foreach (var name in propNames)
            {
                if (elem.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
                    return p.GetString() ?? "";
            }
            return "";
        }
    }

    public static class PiranhaPostSecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UsePiranhaPostSecurityGuard(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PiranhaPostSecurityMiddleware>();
        }
    }
}
