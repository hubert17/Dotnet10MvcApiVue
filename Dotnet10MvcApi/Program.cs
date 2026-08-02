#pragma warning disable CA1416

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Dotnet10MvcApi.Data;
using Dotnet10MvcApi.Models.Entities;
using Dotnet10MvcApi.Services;
using Dotnet10MvcApi.Blazor;
using Dotnet10MvcApi.Services.Cms;
using Microsoft.AspNetCore.Antiforgery;
using Piranha.Manager.Editor;
using Scalar.AspNetCore;
using OpenApi = Microsoft.OpenApi;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// 1. Configure DBContext based on Provider setting
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Jet";
if (dbProvider.Equals("Jet", StringComparison.OrdinalIgnoreCase))
{
    var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
    AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath);

    var connString = builder.Configuration.GetConnectionString("JetConnection");
    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseJet(connString));
    builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
}
else if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    var connString = builder.Configuration.GetConnectionString("PostgreSqlConnection");
    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseNpgsql(connString));
    builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
}

// Configure MvcOptions from "MvcSettings"
builder.Services.Configure<Dotnet10MvcApi.Models.MvcOptions>(builder.Configuration.GetSection("MvcSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Dotnet10MvcApi.Models.MvcOptions>>().Value);

// 2. Configure JWT Bearer Authentication
var secret = builder.Configuration["JwtSettings:Secret"] ?? "f848bcae3399961afba711f8ced6fc3c";
var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "Dotnet10MvcApi";
var audience = builder.Configuration["JwtSettings:Audience"] ?? "Dotnet10MvcApi";

// 2. Configure Authentication (Cookie + JWT Bearer)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.Name = ".AspNetCore.Cookies";
    options.Cookie.Path = "/";
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    options.Events.OnRedirectToLogin = ctx =>
    {
        var returnUrl = ctx.Request.PathBase + ctx.Request.Path + ctx.Request.QueryString;
        var routePrefix = "/" + (builder.Configuration["BlazorSettings:RoutePrefix"] ?? "blazor").Trim('/');
        if (ctx.Request.PathBase.StartsWithSegments(routePrefix) || ctx.Request.Path.StartsWithSegments(routePrefix))
        {
            ctx.Response.Redirect($"{routePrefix}/login?ReturnUrl=" + Uri.EscapeDataString(returnUrl));
        }
        else
        {
            ctx.Response.Redirect("/login?ReturnUrl=" + Uri.EscapeDataString(returnUrl));
        }
        return Task.CompletedTask;
    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    };
});



// 3. Register standard services and native OpenAPI
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
});

// Wrap IAntiforgery in DI to bypass anti-forgery validation for /manager/api routes
var originalAntiforgery = builder.Services.FirstOrDefault(d => d.ServiceType == typeof(IAntiforgery));
if (originalAntiforgery != null)
{
    builder.Services.Remove(originalAntiforgery);
    builder.Services.AddSingleton<IAntiforgery>(sp =>
    {
        IAntiforgery inner;
        if (originalAntiforgery.ImplementationInstance != null)
        {
            inner = (IAntiforgery)originalAntiforgery.ImplementationInstance;
        }
        else if (originalAntiforgery.ImplementationFactory != null)
        {
            inner = (IAntiforgery)originalAntiforgery.ImplementationFactory(sp);
        }
        else
        {
            inner = (IAntiforgery)ActivatorUtilities.CreateInstance(sp, originalAntiforgery.ImplementationType!);
        }
        return new Dotnet10MvcApi.Services.BypassManagerAntiforgery(inner);
    });
}
builder.Services.AddHttpClient();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "Dotnet 10 MVC & API";
        document.Info.Version = "v1";

        // Add JWT Bearer Security Scheme (HTTP Bearer type is preferred in OpenAPI v3)
        var securityScheme = new OpenApi.OpenApiSecurityScheme
        {
            Type = OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = OpenApi.ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
        };

        document.Components ??= new OpenApi.OpenApiComponents();
        if (document.Components.SecuritySchemes == null)
        {
            document.Components.SecuritySchemes = new System.Collections.Generic.Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        }
        document.Components.SecuritySchemes.Add("Bearer", securityScheme);

        // Apply security requirement globally to all endpoints
        var requirement = new OpenApi.OpenApiSecurityRequirement
        {
            {
                new OpenApi.OpenApiSecuritySchemeReference("Bearer", document),
                new System.Collections.Generic.List<string>()
            }
        };

        document.Security ??= new System.Collections.Generic.List<OpenApi.OpenApiSecurityRequirement>();
        document.Security.Add(requirement);

        return Task.CompletedTask;
    });
});

builder.Services.AddScoped<TokenManager>();
builder.Services.AddScoped<DevUserService>();
builder.Services.AddScoped<CmsService>();

// Configure Piranha CMS Services
builder.Services.AddCustomPiranhaCms(builder.Environment);

// Register Blazor Server Components & Services (via BlazorDependencyInjection)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorCore(builder.Configuration);
builder.Services.AddSingleton<Microsoft.AspNetCore.Routing.MatcherPolicy, Dotnet10MvcApi.Services.Blazor.BlazorPathBaseEndpointSelectorPolicy>();

var app = builder.Build();

// Initialize Piranha Content Types
app.UsePiranhaContentTypes();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Blazor sub-path rewriter: strip the configured prefix from the request path BEFORE
// static files and routing run, so Blazor's @page "/" and static assets (_content/...) match correctly.
var blazorPrefix = "/" + (builder.Configuration["BlazorSettings:RoutePrefix"] ?? "blazor").Trim('/');
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments(blazorPrefix, out var remainder))
    {
        ctx.Request.PathBase = ctx.Request.PathBase.Add(blazorPrefix);
        ctx.Request.Path = (remainder.HasValue && !string.IsNullOrEmpty(remainder.Value)) ? remainder : new PathString("/");
    }
    await next();
});

// 4. Set up physical static files (so wwwroot/index.html is served at / only for non-Blazor requests)
app.UseWhen(ctx => string.IsNullOrEmpty(ctx.Request.PathBase), defaultApp =>
{
    defaultApp.UseDefaultFiles();
});
app.UseStaticFiles();

// 5. Setup OpenAPI/Scalar UI
app.MapOpenApi();
app.MapScalarApiReference();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Security Guard: Allow CmsWriter users to view, edit, and save drafts of others' posts, but restrict PUBLISHING to ONLY their own posts and PREVENT duplicate creation attempts.
app.UsePiranhaPostSecurityGuard();

// Ensure Anti-Forgery XSRF-TOKEN cookie & contrast fix CSS are populated on /manager requests
app.UsePiranhaManagerAssets();

// Enable Piranha CMS Middleware (bypassed for /blazor requests so Blazor Server endpoints take priority)
app.UseWhen(ctx => !ctx.Request.PathBase.StartsWithSegments(blazorPrefix) && !ctx.Request.Path.StartsWithSegments(blazorPrefix), piranhaApp =>
{
    piranhaApp.UsePiranha(options =>
    {
        // Configure TinyMCE editor toolbar
        EditorConfig.FromFile("editorconfig.json");
        options.UseManager();
        options.UseTinyMCE();
    });
});

// 6. Map controllers (APIs + MVC routing)
app.MapControllers();

// Serve wwwroot/app/index.html at /app
app.MapGet("/app", async context =>
{
    var indexPath = Path.Combine(app.Environment.WebRootPath, "app", "index.html");
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync(indexPath);
});

// Map Blazor Server Components. Path-rewrite middleware sets PathBase=/blazor and rewrites Path to /.
app.MapRazorComponents<Dotnet10MvcApi.Blazor.App>()
    .AddInteractiveServerRenderMode();

app.MapHub<Dotnet10MvcApi.Services.Notifications.NotificationHub>("/notificationhub");
app.MapHub<Dotnet10MvcApi.Services.Notifications.ChatHub>("/chathub");

var mvcHomeRoute = builder.Configuration["MvcSettings:HomeRoute"]?.Trim('/');
if (!string.IsNullOrWhiteSpace(mvcHomeRoute))
{
    app.MapControllerRoute(
        name: "portalHome",
        pattern: mvcHomeRoute,
        defaults: new { controller = "Home", action = "Index" });
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Redirect legacy /swagger path to /scalar/v1
app.MapGet("/swagger", context =>
{
    context.Response.Redirect("/scalar/v1");
    return System.Threading.Tasks.Task.CompletedTask;
});

// 7. Auto-migrate database and seed tables on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Run EF Core Migrations
        db.Database.Migrate();
        Console.WriteLine("Database migrated successfully.");

        // Create [#Dual] table needed by EF Core Jet provider for Any() and other scalar queries
        if (dbProvider.Equals("Jet", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                db.Database.ExecuteSqlRaw("CREATE TABLE [#Dual] (Id INT)");
                db.Database.ExecuteSqlRaw("INSERT INTO [#Dual] (Id) VALUES (1)");
                Console.WriteLine("Created [#Dual] table successfully.");
            }
            catch { /* Already exists */ }
        }

        // Seed default admin account (admin/admin)
        var devUserService = scope.ServiceProvider.GetRequiredService<DevUserService>();
        var userAccountService = new UserAccountService(db, devUserService);
        userAccountService.EnsureAdminExistsAsync().GetAwaiter().GetResult();
        Console.WriteLine("Seeded default admin account (admin) successfully.");

        // Seed Product table if empty
        if (!db.Products.Any())
        {
            db.Products.AddRange(Product.SeedData());
            db.SaveChanges();
            Console.WriteLine("Seeded Products table successfully.");
        }

        // Seed Songs table if empty
        if (!db.Songs.Any())
        {
            Song.Seed(db, clearSongTable: false);
            Console.WriteLine("Seeded Songs table successfully from Billboard CSV.");
        }

        // Seed Piranha CMS initial content (Blogs & Articles)
        try
        {
            var piranhaApi = scope.ServiceProvider.GetRequiredService<Piranha.IApi>();
            CmsContentSeeder.SeedAsync(piranhaApi).GetAwaiter().GetResult();
        }
        catch (Exception piranhaEx)
        {
            Console.WriteLine($"Piranha CMS Startup Seeding Warning: {piranhaEx.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database Migration/Seeding Warning: {ex.Message}");
    }
}

app.Run();
