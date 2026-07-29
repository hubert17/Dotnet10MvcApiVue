# Modernized .NET 10 Multi-Paradigm Web Platform

[![Framework](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Unified%20Layered%20Monolith-38bdf8?style=flat-square)](https://github.com/hubert17/Dotnet10MvcApiVue)
[![Database](https://img.shields.io/badge/Database-EF%20Core%20Jet%20%2B%20SQLite-4ade80?style=flat-square)](https://github.com/hubert17/Dotnet10MvcApiVue)
[![Author](https://img.shields.io/badge/Created%20By-Bernard%20Gabon-c084fc?style=flat-square)](https://github.com/hubert17)

A state-of-the-art **ASP.NET Core .NET 10.0** application hosting **6 distinct web application paradigms** simultaneously out of a single process. Built as a zero-build, unified layered monolith combining static landing pages, server-rendered Razor MVC, zero-node Vue 2.x SPAs, real-time Blazor Server with MudBlazor, Piranha CMS v12 editorial content publishing, and controller-based REST APIs.

---

## ⚡ The 6 Web Application Paradigms

This platform demonstrates how to host multiple frontend paradigms inside a single ASP.NET Core process without node build servers or microservice overhead.

```text
                                  ┌── GET / ───────────────► Static Landing Page (wwwroot/index.html)
                                  ├── GET /home ───────────► ASP.NET Core MVC (SSR + Petite-Vue)
                                  ├── GET /app ────────────► Vue 2.x SPA (Zero-Build ES Modules)
ASP.NET Core .NET 10 Monolith ────┼── GET /blazor ─────────► Blazor Server SPA (MudBlazor + SignalR)
                                  ├── GET /blogs, /articles► Piranha CMS v12 Editorial Engine
                                  └── GET /manager, /api ──► CMS Admin Portal & REST APIs (Scalar)
```

### 📊 Paradigm Comparison & Usage Matrix

| Paradigm | Route Prefix | Primary Tech Stack | Best Used For (When to Use) | Avoid For (When NOT to Use) |
| :--- | :--- | :--- | :--- | :--- |
| **Static Portal** | `GET /` | HTML5 / CSS3 / Glassmorphism | Sub-millisecond marketing portals, documentation hubs, zero-latency initial entries. | Dynamic stateful pages requiring database binding or auth. |
| **MVC (SSR)** | `GET /home` | Razor Views + Petite-Vue | SEO-critical public web portals, e-commerce, transactional form workflows. | Desktop-grade SPAs requiring continuous fluid UI transitions. |
| **Vue 2 SPA** | `GET /app` | ES Modules + Vuetify + Vuex | High-traffic, high-concurrency client dashboards with zero node build overhead. | Content-heavy pages dependent on public web search crawlers. |
| **Blazor Server** | `GET /blazor` | C# + MudBlazor + SignalR | Internal backoffice applications, admin tools, and intranet SPAs using full C# logic. | Public sites with tens of thousands of concurrent WebSocket circuits. |
| **Piranha CMS** | `GET /blogs`, `/articles` | Piranha CMS v12 + SQLite | Editorial blogs, news releases, tech articles, and content managed by non-devs. | Custom transactional business forms or operational APIs. |
| **Admin & APIs** | `GET /manager`, `/api` | REST Controllers + JWT + Scalar | Site content administration, mobile client backends, and programmatic integrations. | General end-user web application browsing. |

---

## 🚀 Architectural & Engineering Highlights

### 1. Dynamic Blazor Route Prefixing & Sub-Path Rewriting
*   **Configurable Prefix:** Blazor Server is configured via `appsettings.json` (`BlazorSettings:RoutePrefix`). Changing `"blazor"` to `"app2"`, `"portal"`, etc. dynamically updates route paths, challenge redirects, and circuit initialization.
*   **Middleware Pipeline Sequencing:** Sub-path rewriting runs **before** `app.UseStaticFiles()`, allowing static web assets (like `_content/MudBlazor/...`) under sub-paths to return `HTTP 200 OK`.
*   **Root Protection:** `app.UseDefaultFiles()` is scoped via `app.UseWhen(ctx => string.IsNullOrEmpty(ctx.Request.PathBase))` so `wwwroot/index.html` is served exclusively on root domain requests (`/`) without intercepting Blazor sub-paths (`/app2`).

### 2. Unified Hybrid Authentication & Circuit State Preservation
*   **Unified Cookie Scheme:** Shared Cookie authentication (`.AspNetCore.Cookies` configured with `options.Cookie.Path = "/"`) grants single sign-on across MVC Razor views (`/home`) and Blazor Server (`/blazor`).
*   **SignalR Circuit Auth Preservation:** `ServerCookieAuthService` captures the HTTP GET `HttpContext.User` during initial connection and preserves the `ClaimsPrincipal` across SignalR WebSocket DI circuit scopes where `IHttpContextAccessor` is null.
*   **Static SSR Auth Routes:** Blazor `/login` and `/logout` pages render in Static SSR mode (`RenderMode = null`) to ensure HTTP `Set-Cookie` headers are properly dispatched to browsers.
*   **Dual Authentication Pipeline:** Controller APIs (`/api/...`) use JWT Bearer Security (`[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`) with refresh token revocation.

### 3. MS Access Database Provider (Jet / EF Core) & SQLite Engine
*   **Relational Access Engine:** Application relational data uses `EntityFrameworkCore.Jet` targeting MS Access (`MyAccessDb.mdb`), prepared for migration to PostgreSQL.
*   **Piranha CMS Engine:** Editorial data uses `Piranha.Data.EF.SQLite` targeting `App_Data/piranha.db`.
*   **`#Dual` Scalar Query Support:** Automatically checks and seeds the single-row `[#Dual]` helper table on startup to support Jet LINQ evaluations (such as `.Any()`).
*   **High-Speed ADO.NET Bulk Seeding:** Inserts 10,000+ Billboard song records in **under 2 seconds** using raw parameterized ADO.NET SQL commands inside a single transaction, bypassing EF Core change-tracking overhead.

### 4. Interactive API Documentation & HTML-First Views
*   **Scalar OpenAPI Playground:** Visual API playground served at `/scalar/v1` (with `/swagger` redirected automatically).
*   **CDN-First Delivery:** Front-end libraries (Bootstrap 4, Font Awesome, MudBlazor, Petite-Vue, Vue 2, Vuetify) are served directly via **jsDelivr CDN**.

---

## 🛠️ Windows-on-ARM64 & x64 Run Requirements

Because the application connects to an MS Access database via OLE DB drivers (`Microsoft.ACE.OLEDB`), which are compiled exclusively for x86/x64 architectures:

> [!IMPORTANT]
> **x64 Emulation Constraint:** Always build, run, or debug the project specifying the `--arch x64` flag:
> ```powershell
> dotnet run --project Dotnet10MvcApi --arch x64
> ```

### Launcher Helper Script (`run-debug.bat`)
*   **Standard Interactive Run:**
    ```powershell
    .\Dotnet10MvcApi\run-debug.bat
    ```
*   **Low-Verbosity Agent Run:**
    ```powershell
    .\Dotnet10MvcApi\run-debug.bat --agent
    ```

---

## 📂 Project Directory Structure

```text
Dotnet10MvcApiVue/
├── Dotnet10MvcApi/
│   ├── App_Data/
│   │   ├── MyAccessDb.mdb               # Relational MS Access Database
│   │   ├── piranha.db                   # Piranha CMS SQLite Database
│   │   └── BillboardTo2013.zip          # Billboard Songs Dataset Zip
│   ├── Blazor/                          # Blazor Server Architecture
│   │   ├── Components/                  # Shared Blazor Components & Demos
│   │   ├── Layout/                      # MainLayout & NavMenu
│   │   ├── Pages/                       # Blazor Pages (Home, Auth, Weather, Counter, Products)
│   │   │   └── Account/                 # Login, Register, Logout, ChangePassword (Static SSR)
│   │   ├── States/                      # BlazorState State Containers
│   │   ├── App.razor                    # Root Component & Static SSR Router
│   │   ├── BlazorOptions.cs             # Dynamic Sub-Path Route Configuration
│   │   └── Routes.razor                 # Cascading Authentication Router
│   ├── Controllers/
│   │   ├── Api/                         # REST API Controllers (JWT Auth)
│   │   │   ├── AccountController.cs     # /TOKEN, /TOKENREFRESH, Register
│   │   │   ├── SampleController.cs      # Weather, Email, Multipart File Upload
│   │   │   └── SongController.cs       # Billboard Songs Paged API
│   │   └── Mvc/                         # Server-Rendered MVC Controllers (Cookie Auth)
│   │       ├── AccountController.cs     # Razor Login, Register, Profile
│   │       ├── CrudsampleController.cs  # Product CRUD Portal
│   │       └── HomeController.cs        # MVC Home Portal
│   ├── Data/
│   │   └── ApplicationDbContext.cs      # EF Core Access DB Context
│   ├── Helpers/
│   │   └── ImageUploadExtension.cs      # GDI+ Image Thumbnail Generator
│   ├── Models/
│   │   ├── Cms/                         # Piranha CMS Pages, Posts & HeroBlock
│   │   ├── Dtos/                        # Data Transfer Objects
│   │   └── Entities/                    # Product, Song, UserAccount Entities
│   ├── Services/
│   │   ├── Blazor/
│   │   │   ├── BlazorPathBaseEndpointSelectorPolicy.cs  # Endpoint Disambiguator
│   │   │   └── ServerCookieAuthService.cs              # SignalR Circuit Auth Preserver
│   │   ├── Cms/
│   │   │   └── CmsContentSeeder.cs      # Initial Piranha CMS Seeder
│   │   └── TokenManager.cs              # JWT Security Service
│   ├── Views/                           # MVC & Piranha CMS Razor View Templates
│   ├── wwwroot/                         # Public Static Web Root
│   │   ├── app/                         # Vue 2.x SPA (Zero-Build ES Modules)
│   │   ├── js/                          # Custom JS Utilities (geo.js, scroll.js)
│   │   └── index.html                   # Multi-Paradigm Glassmorphic Landing Portal
│   ├── Program.cs                       # Middleware Pipeline & Service Registration
│   ├── run-debug.bat                    # x64 Architecture Launcher Script
│   └── appsettings.json                 # Connection Strings & BlazorSettings
└── README.md                            # Documentation
```

---

## 🏁 Getting Started

### Prerequisites
*   **.NET 10.0 SDK**
*   **Microsoft Access Database Engine 2016 Redistributable (x64)**

### Build the Application
```powershell
dotnet build Dotnet10MvcApi/Dotnet10MvcApi.csproj --arch x64
```

### Run the Application
```powershell
.\Dotnet10MvcApi\run-debug.bat
```

### Key Endpoints
*   **Static Landing Portal:** [https://localhost:7031/](https://localhost:7031/)
*   **ASP.NET Core MVC Portal:** [https://localhost:7031/home](https://localhost:7031/home)
*   **Vue 2.x SPA:** [https://localhost:7031/app](https://localhost:7031/app)
*   **Blazor Server SPA:** [https://localhost:7031/blazor](https://localhost:7031/blazor) (or configured `RoutePrefix`)
*   **Piranha CMS Blogs:** [https://localhost:7031/blogs](https://localhost:7031/blogs)
*   **Piranha CMS Articles:** [https://localhost:7031/articles](https://localhost:7031/articles)
*   **Piranha CMS Admin Manager:** [https://localhost:7031/manager](https://localhost:7031/manager)
*   **Scalar OpenAPI API Docs:** [https://localhost:7031/scalar/v1](https://localhost:7031/scalar/v1)

---

## 👤 Author & Repository

*   **Repository:** [https://github.com/hubert17/Dotnet10MvcApiVue](https://github.com/hubert17/Dotnet10MvcApiVue)
*   **Author:** [Bernard Gabon (hubert17)](https://github.com/hubert17)
*   **License:** MIT
