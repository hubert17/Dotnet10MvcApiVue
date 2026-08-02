using System.IO;
using Dotnet10MvcApi.Models.Cms.Blocks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Piranha;
using Piranha.AttributeBuilder;
using Piranha.Data.EF.SQLite;

namespace Dotnet10MvcApi.Services.Cms
{
    /// <summary>
    /// Extension methods for configuring Piranha CMS services and initialization in ASP.NET Core pipeline.
    /// </summary>
    public static class PiranhaCmsExtensions
    {
        public static IServiceCollection AddCustomPiranhaCms(this IServiceCollection services, IWebHostEnvironment environment)
        {
            services.AddPiranha(options =>
            {
                options.AddRazorRuntimeCompilation = true;

                var piranhaDbPath = Path.Combine(environment.ContentRootPath, "App_Data", "piranha.db");
                options.UseEF<SQLiteDb>(db =>
                    db.UseSqlite($"Data Source={piranhaDbPath}"));
                options.UseManager();
                options.UseTinyMCE();
                options.UseMemoryCache();
                options.UseImageSharp();
                options.UseFileStorage(basePath: "wwwroot/cms/uploads/", baseUrl: "~/cms/uploads/", naming: Piranha.Local.FileStorageNaming.UniqueFolderNames);
            });

            // Register Piranha Manager security bridge (LocalAuth ISecurity)
            services.AddScoped<Piranha.Manager.LocalAuth.ISecurity, PiranhaManagerSecurity>();

            // Configure Piranha Manager Authorization Policies for Standardized Roles
            services.AddPiranhaAuthorizationPolicies();

            return services;
        }

        public static IServiceCollection AddPiranhaAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                foreach (var permission in Piranha.Manager.Permission.All())
                {
                    options.AddPolicy(permission, policy =>
                    {
                        switch (permission)
                        {
                            // Entry permission into manager portal
                            case Piranha.Manager.Permission.Admin:
                                policy.RequireRole("admin", "Admin", "CmsEditor", "editor", "Editor", "CmsWriter", "writer", "Writer", "CmsModerator", "moderator", "Moderator", "comment moderator");
                                break;

                            // Pages Navigation (All CMS Roles)
                            case Piranha.Manager.Permission.Pages:
                                policy.RequireRole("admin", "Admin", "CmsEditor", "editor", "Editor", "CmsWriter", "writer", "Writer", "CmsModerator", "moderator", "Moderator");
                                break;

                            // Pages View & Edit (CmsWriter, CmsModerator, CmsEditor & Admins)
                            case Piranha.Manager.Permission.PagesEdit:
                            case Piranha.Manager.Permission.PagesSave:
                                policy.RequireRole("admin", "Admin", "CmsEditor", "editor", "Editor", "CmsWriter", "writer", "Writer", "CmsModerator", "moderator", "Moderator");
                                break;

                            // Pages Structure & Publishing (CmsEditor & Admins)
                            case Piranha.Manager.Permission.PagesAdd:
                            case Piranha.Manager.Permission.PagesPublish:
                            case Piranha.Manager.Permission.PagesDelete:
                                policy.RequireRole("admin", "Admin", "CmsEditor", "editor", "Editor");
                                break;

                            // Posts Drafting, Editing & Publishing (CmsWriter for own posts, CmsEditor & Admins)
                            case Piranha.Manager.Permission.Posts:
                            case Piranha.Manager.Permission.PostsAdd:
                            case Piranha.Manager.Permission.PostsEdit:
                            case Piranha.Manager.Permission.PostsSave:
                            case Piranha.Manager.Permission.PostsPublish:
                                policy.RequireRole("admin", "Admin", "CmsEditor", "editor", "Editor", "CmsWriter", "writer", "Writer");
                                break;

                            // Posts Deleting (CmsEditor & Admins)
                            case Piranha.Manager.Permission.PostsDelete:
                                policy.RequireRole("admin", "Admin", "CmsEditor", "editor", "Editor");
                                break;

                            // Media Assets Management (CmsWriter, CmsModerator, CmsEditor & Admins)
                            case Piranha.Manager.Permission.Media:
                            case Piranha.Manager.Permission.MediaAdd:
                            case Piranha.Manager.Permission.MediaEdit:
                                policy.RequireRole("admin", "Admin", "CmsEditor", "editor", "Editor", "CmsWriter", "writer", "Writer", "CmsModerator", "moderator", "Moderator");
                                break;
                            case Piranha.Manager.Permission.MediaDelete:
                                policy.RequireRole("admin", "Admin", "CmsEditor", "editor", "Editor");
                                break;

                            // Comments Moderation (CmsModerator, CmsEditor & Admins)
                            case Piranha.Manager.Permission.Comments:
                            case Piranha.Manager.Permission.CommentsApprove:
                            case Piranha.Manager.Permission.CommentsDelete:
                                policy.RequireRole("admin", "Admin", "CmsEditor", "editor", "Editor", "CmsModerator", "moderator", "Moderator");
                                break;

                            // System & Site Administration (Admins Only)
                            case Piranha.Manager.Permission.Aliases:
                            case Piranha.Manager.Permission.Sites:
                            case Piranha.Manager.Permission.Modules:
                            default:
                                policy.RequireRole("admin", "Admin");
                                break;
                        }
                    });
                }
            });

            return services;
        }

        public static IApplicationBuilder UsePiranhaContentTypes(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IApi>();
            App.Init(api);
            App.Blocks.Register<HeroBlock>();
            App.Blocks.Register<YouTubeBlock>();
            new ContentTypeBuilder(api)
                .AddAssembly(typeof(PiranhaCmsExtensions).Assembly)
                .Build();

            return app;
        }
    }
}
