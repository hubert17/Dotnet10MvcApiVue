using System;
using System.Linq;
using System.Threading.Tasks;
using Dotnet10MvcApi.Models.Cms;
using Dotnet10MvcApi.Models.Cms.Blocks;
using Piranha;
using Piranha.Models;
using Piranha.Extend.Blocks;

namespace Dotnet10MvcApi.Services.Cms
{
    public static class CmsContentSeeder
    {
        public static async Task SeedAsync(IApi api)
        {
            try
            {
                // Ensure default site exists
                var site = await api.Sites.GetDefaultAsync();
                if (site == null) return;

                // 1. Get or create Blog Archive Page
                var blogPage = await api.Pages.GetBySlugAsync<BlogArchivePage>("blogs", site.Id);
                Guid blogPageId;
                if (blogPage == null)
                {
                    blogPage = await BlogArchivePage.CreateAsync(api);
                    blogPage.SiteId = site.Id;
                    blogPage.Title = "Blogs";
                    blogPage.NavigationTitle = "Blogs";
                    blogPage.Slug = "blogs";
                    blogPage.MetaTitle = "Platform Insights & Tech Blog";
                    blogPage.MetaDescription = "Read the latest engineering articles on ASP.NET Core, Vue.js, and Piranha CMS.";
                    blogPage.Published = DateTime.Now;

                    await api.Pages.SaveAsync(blogPage);
                    blogPageId = blogPage.Id;
                }
                else
                {
                    blogPageId = blogPage.Id;
                }

                // 2. Get or create Article Archive Page
                var articlePage = await api.Pages.GetBySlugAsync<ArticleArchivePage>("articles", site.Id);
                Guid articlePageId;
                if (articlePage == null)
                {
                    articlePage = await ArticleArchivePage.CreateAsync(api);
                    articlePage.SiteId = site.Id;
                    articlePage.Title = "Articles";
                    articlePage.NavigationTitle = "Articles";
                    articlePage.Slug = "articles";
                    articlePage.MetaTitle = "Technical Knowledge Base & Guides";
                    articlePage.MetaDescription = "In-depth technical guides for building enterprise ASP.NET Core applications.";
                    articlePage.Published = DateTime.Now;

                    await api.Pages.SaveAsync(articlePage);
                    articlePageId = articlePage.Id;
                }
                else
                {
                    articlePageId = articlePage.Id;
                }

                // 3. Seed initial blog posts if blog archive is empty
                var existingBlogPosts = await api.Posts.GetAllAsync<BlogPost>(blogPageId);
                if (existingBlogPosts == null || !existingBlogPosts.Any())
                {
                    // Seed Blog Post 1
                    var post1 = await BlogPost.CreateAsync(api);
                    post1.BlogId = blogPageId;
                    post1.Title = "Welcome to Dotnet10 Multi-Paradigm Platform";
                    post1.Slug = "welcome-to-dotnet10-platform";
                    post1.Category = "Technology";
                    post1.Tags.Add("Dotnet");
                    post1.Tags.Add("Architecture");
                    post1.Tags.Add("PiranhaCMS");
                    post1.Subtitle = "A unified ASP.NET Core monolith hosting Static, Vue SPA, Razor MVC, REST Web APIs, and Piranha CMS.";
                    post1.Excerpt = "Welcome to our platform engineering blog! Discover how ASP.NET Core .NET 10 hosts 4 web application paradigms simultaneously in a single codebase.";
                    post1.AuthorName = "Bernard Gabon";
                    post1.Published = DateTime.Now.AddDays(-2);

                    post1.EnableComments = true;
                    post1.RequireModeration.Value = true;
                    post1.Blocks.Add(new HtmlBlock
                    {
                        Body = @"<p class='lead text-light'>Welcome to our official platform engineering blog! This application is engineered as a unified ASP.NET Core monolith, serving static landing pages, reactive Vue 2.x single page applications, server-side rendered Razor MVC views, JWT-secured REST APIs, and dynamic Piranha CMS content simultaneously.</p>
                                
                                <h3 class='text-white mt-4 mb-3'>1. Core Architecture Principles</h3>
                                <p>Building modern enterprise applications often tempts teams to separate frontends and backends into disparate repositories and build servers. In this project, we demonstrate a unified monolith design that minimizes complexity while supporting four distinct web paradigms in a single process.</p>
                                
                                <h3 class='text-white mt-4 mb-3'>2. Multi-Paradigm Capabilities</h3>
                                <ul>
                                    <li><strong>Static Root:</strong> High speed landing page served from static web root.</li>
                                    <li><strong>Vue 2.x SPA:</strong> Zero-build reactive single page application using native ES Modules.</li>
                                    <li><strong>Razor MVC:</strong> Traditional server-side rendered pages with Petite-Vue reactivity.</li>
                                    <li><strong>Web APIs & Scalar:</strong> OpenAPI specification playground powered by Scalar.</li>
                                </ul>

                                <h3 class='text-white mt-4 mb-3'>3. Conclusion</h3>
                                <p>By eliminating Node build pipelines while leveraging standard ASP.NET Core patterns, developer velocity is maximized with zero build friction.</p>"
                    });
                    await api.Posts.SaveAsync(post1);
                    await api.Posts.SaveCommentAsync(post1.Id, new PostComment
                    {
                        Author = "Jane Doe",
                        Email = "jane@example.com",
                        Body = "Great article on multi-paradigm architecture! The unified monolith approach is very clean.",
                        IsApproved = true,
                        Created = DateTime.Now.AddDays(-1)
                    });

                    // Seed Blog Post 2
                    var post2 = await BlogPost.CreateAsync(api);
                    post2.BlogId = blogPageId;
                    post2.Title = "Building Modern Vue 2.x SPA in Monolith Apps";
                    post2.Slug = "building-vue2-spa-monolith";
                    post2.Category = "Frontend";
                    post2.Tags.Add("Vue");
                    post2.Tags.Add("JavaScript");
                    post2.Tags.Add("SPA");
                    post2.Subtitle = "Zero-build native ES module integration with Petite-Vue reactivity.";
                    post2.Excerpt = "Learn how to build lightweight Vue 2.x applications directly in ASP.NET Core without Webpack or Node.js build steps.";
                    post2.AuthorName = "Bernard Gabon";
                    post2.Published = DateTime.Now.AddDays(-1);
                    post2.EnableComments = true;
                    post2.RequireModeration.Value = true;

                    post2.Blocks.Add(new HtmlBlock
                    {
                        Body = @"<p class='lead text-light'>Modern web development often introduces heavy build pipelines (Webpack, Vite, Node modules). In this project, we demonstrate native browser ES Module imports that deliver instant loading speeds without any Node build servers.</p>

                                <h3 class='text-white mt-4 mb-3'>1. Why Native ES Modules?</h3>
                                <p>By referencing CDN-delivered modules directly in <code>wwwroot/app/index.html</code>, frontend components load natively in modern browsers with zero compilation overhead.</p>

                                <h3 class='text-white mt-4 mb-3'>2. Lightweight Client Reactivity</h3>
                                <p>For server-rendered MVC pages, we utilize <strong>Petite-Vue</strong> for minor DOM reactivity, form validation feedback, and dynamic toggle behaviors without heavy runtime JS overhead.</p>

                                <h3 class='text-white mt-4 mb-3'>3. Conclusion</h3>
                                <p>Combining native ES modules with Petite-Vue provides the sweet spot of rapid frontend reactivity without the burden of complex build tooling.</p>"
                    });
                    await api.Posts.SaveAsync(post2);

                    Console.WriteLine("Seeded initial Piranha CMS Blog Posts successfully.");
                }

                // 4. Seed initial articles if article archive is empty
                var existingArticles = await api.Posts.GetAllAsync<ArticlePost>(articlePageId);
                if (existingArticles == null || !existingArticles.Any())
                {
                    // Seed Article 1
                    var article1 = await ArticlePost.CreateAsync(api);
                    article1.BlogId = articlePageId;
                    article1.Title = "Comprehensive Guide to Modern ASP.NET Core & Hybrid Auth";
                    article1.Slug = "guide-to-aspnetcore-hybrid-auth";
                    article1.Category = "Architecture";
                    article1.Tags.Add("ASP.NET");
                    article1.Tags.Add("Security");
                    article1.Tags.Add("OAuth");
                    article1.Subtitle = "Combining Cookie authentication for SSR with JWT Bearer tokens for REST Web APIs.";
                    article1.Excerpt = "An in-depth guide on implementing hybrid cookie and bearer token authentication in ASP.NET Core.";
                    article1.AuthorName = "Bernard Gabon";
                    article1.ReadingTime = "5 mins";
                    article1.Published = DateTime.Now.AddDays(-3);
                    article1.EnableComments = false;
                    article1.Citations = "<ol><li>Microsoft Docs: <a href='https://learn.microsoft.com/aspnet/core/security/' target='_blank' class='text-info'>ASP.NET Core Security Guidelines</a></li><li>IETF RFC 7519: <a href='https://datatracker.ietf.org/doc/html/rfc7519' target='_blank' class='text-info'>JSON Web Token Specification</a></li></ol>";

                    article1.Blocks.Add(new HtmlBlock
                    {
                        Body = @"<p class='lead text-light'>In modern web applications, client applications (mobile apps, SPAs, external API consumers) require token-based authentication (JWT Bearer), while traditional server-rendered browser pages rely on Cookie sessions. ASP.NET Core allows registering multiple authentication schemes side by side.</p>

                                <h3 class='text-white mt-4 mb-3'>1. Configuring Dual Authentication</h3>
                                <p>By setting Cookie authentication as default while explicitly decorating REST API controllers with <code>[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]</code>, both web browsers and API consumers are seamlessly authenticated.</p>

                                <h3 class='text-white mt-4 mb-3'>2. Security Claims & Roles</h3>
                                <p>The system registers both standard User and Admin roles. Administrators logging into the site are granted full Piranha CMS Manager claims for site management.</p>

                                <h3 class='text-white mt-4 mb-3'>3. Conclusion</h3>
                                <p>Hybrid authentication gives you the best of both worlds: secure browser session cookies for SSR pages and bearer tokens for stateless REST endpoints.</p>"
                    });
                    await api.Posts.SaveAsync(article1);

                    // Seed Article 2
                    var article2 = await ArticlePost.CreateAsync(api);
                    article2.BlogId = articlePageId;
                    article2.Title = "Optimizing Performance for Dual Web API and SSR Applications";
                    article2.Slug = "optimizing-performance-dual-webapi-ssr";
                    article2.Category = "Performance";
                    article2.Tags.Add("Performance");
                    article2.Tags.Add("Caching");
                    article2.Tags.Add("EFCore");
                    article2.Subtitle = "EF Core query optimizations, scalar queries, and lightweight CDN asset loading.";
                    article2.Excerpt = "Practical techniques for optimizing web application throughput, query speeds, and front-end asset loading.";
                    article2.AuthorName = "Bernard Gabon";
                    article2.ReadingTime = "7 mins";
                    article2.Published = DateTime.Now.AddDays(-1);
                    article2.EnableComments = false;
                    article2.Citations = "<ol><li>EntityFrameworkCore.Jet Provider Documentation and `#Dual` query translation rules.</li><li>jsDelivr Public CDN asset caching and distribution patterns.</li></ol>";

                    article2.Blocks.Add(new HtmlBlock
                    {
                        Body = @"<p class='lead text-light'>Performance optimization in ASP.NET Core requires attention to both backend database execution and frontend asset delivery networks.</p>

                                <h3 class='text-white mt-4 mb-3'>1. Database Query Optimization</h3>
                                <p>Using EF Core Jet with MS Access requires specific optimizations such as raw ADO.NET batch execution for large bulk inserts and maintaining single-row helper tables like <code>[#Dual]</code> for scalar LINQ translation.</p>

                                <h3 class='text-white mt-4 mb-3'>2. CDN-First Asset Delivery</h3>
                                <p>All front-end vendor libraries (Bootstrap, Petite-Vue, FontAwesome) are loaded via public CDN (jsDelivr) to ensure high cache hit rates and minimal local server payload.</p>

                                <h3 class='text-white mt-4 mb-3'>3. Conclusion</h3>
                                <p>Optimizing both backend database queries and client asset delivery yields maximum application throughput and minimal latency.</p>"
                    });
                    await api.Posts.SaveAsync(article2);

                    Console.WriteLine("Seeded initial Piranha CMS Articles successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding Piranha CMS content: {ex.Message}");
            }
        }
    }
}
