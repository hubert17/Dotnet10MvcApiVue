#pragma warning disable CA1416

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Dotnet10MvcApi.Data;
using Dotnet10MvcApi.Helpers;
using Dotnet10MvcApi.Models;
using Dotnet10MvcApi.Models.Entities;
using Dotnet10MvcApi.Services;
using Dotnet10MvcApi.Models.Cms;
using Dotnet10MvcApi.Services.Cms;
using Piranha;
using Piranha.AttributeBuilder;
using Piranha.Data.EF.SQLite;
using Piranha.Manager.Editor;
using Scalar.AspNetCore;
using OpenApi = Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure DBContext based on Provider setting
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Jet";
if (dbProvider.Equals("Jet", StringComparison.OrdinalIgnoreCase))
{
    var appDataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
    AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath);

    var connString = builder.Configuration.GetConnectionString("JetConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseJet(connString));
}
else if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    var connString = builder.Configuration.GetConnectionString("PostgreSqlConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connString));
}

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
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logoff";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
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
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    };
});

// 3. Register standard services and native OpenAPI
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "XSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = false;
});
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

// Configure Piranha CMS
builder.Services.AddPiranha(options =>
{
    options.AddRazorRuntimeCompilation = true;

    var piranhaDbPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "piranha.db");
    options.UseEF<SQLiteDb>(db =>
        db.UseSqlite($"Data Source={piranhaDbPath}"));
    options.UseManager();
    options.UseTinyMCE();
    options.UseMemoryCache();
    options.UseImageSharp();
    options.UseFileStorage(naming: Piranha.Local.FileStorageNaming.UniqueFolderNames);
});

// Register the Piranha Manager security bridge (LocalAuth ISecurity)
// This allows the manager's login/save/publish to delegate to our cookie auth
builder.Services.AddScoped<Piranha.Manager.LocalAuth.ISecurity, Dotnet10MvcApi.Services.PiranhaManagerSecurity>();

var app = builder.Build();

// Initialize Piranha Content Types
using (var scope = app.Services.CreateScope())
{
    var api = scope.ServiceProvider.GetRequiredService<IApi>();
    App.Init(api);
    App.Blocks.Register<Dotnet10MvcApi.Models.Cms.Blocks.HeroBlock>();
    new ContentTypeBuilder(api)
        .AddAssembly(typeof(Program).Assembly)
        .Build();
}

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

// 4. Set up physical static files (so wwwroot/index.html is served at /)
app.UseDefaultFiles();
app.UseStaticFiles();

// 5. Setup OpenAPI/Scalar UI
app.MapOpenApi();
app.MapScalarApiReference();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Enable Piranha CMS Middleware
app.UsePiranha(options =>
{
    // Configure TinyMCE editor toolbar
    EditorConfig.FromFile("editorconfig.json");
    options.UseManager();
    options.UseTinyMCE();
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
        
        // This will run the migration (creating RefreshTokens table)
        db.Database.Migrate();

        // Create [#Dual] table needed by EF Core Jet provider for Any() and other scalar queries
        try
        {
            db.Database.ExecuteSqlRaw("CREATE TABLE [#Dual] (Id INT)");
            db.Database.ExecuteSqlRaw("INSERT INTO [#Dual] (Id) VALUES (1)");
            Console.WriteLine("Created [#Dual] table successfully.");
        }
        catch { /* Already exists */ }

        // Print existing tables for diagnostics
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
        var dt = conn.GetSchema("Tables");
        Console.WriteLine("TABLES IN ACCESS DATABASE:");
        foreach (System.Data.DataRow row in dt.Rows)
        {
            var tableName = row["TABLE_NAME"].ToString();
            var tableType = row["TABLE_TYPE"].ToString();
            if (tableType == "TABLE")
            {
                Console.WriteLine($"- {tableName}");
            }
        }

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
