# Modernized .NET 10 Multi-Paradigm Web Platform

[![Framework](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Unified%20Layered%20Monolith-38bdf8?style=flat-square)](https://github.com/hubert17/Dotnet10MvcApiVue)
[![Database](https://img.shields.io/badge/Database-EF%20Core%20PostgreSQL%20%2B%20SQLite-4ade80?style=flat-square)](https://github.com/hubert17/Dotnet10MvcApiVue)
[![Author](https://img.shields.io/badge/Created%20By-Bernard%20Gabon-c084fc?style=flat-square)](https://github.com/hubert17)

A state-of-the-art **ASP.NET Core .NET 10.0** application hosting **6 distinct web application paradigms** simultaneously out of a single process. Built as a zero-build, unified layered monolith combining static landing pages, server-rendered Razor MVC, zero-node Vue 2.x SPAs, real-time Blazor Server with MudBlazor, Piranha CMS v12 editorial content publishing, and controller-based REST APIs.

---

## ⚡ The 6 Web Application Paradigms

This platform demonstrates how to host multiple frontend paradigms inside a single ASP.NET Core process without node build servers or microservice overhead.

```text
                                  ┌── GET / ───────────────► Static Landing Page / Home Page (wwwroot/index.html)
                                  ├── GET /portal ─────────► ASP.NET Core MVC Portal (SSR + Petite-Vue)
                                  ├── GET /app ────────────► Vue 2.x SPA (Zero-Build ES Modules)
ASP.NET Core .NET 10 Monolith ────┼── GET /blazor ─────────► Blazor Server SPA (MudBlazor + SignalR)
                                  ├── GET /blogs, /articles► Piranha CMS v12 Editorial Engine
                                  └── GET /manager, /api ──► CMS Admin Portal & REST APIs (Scalar)
```

### 📊 Paradigm Comparison & Usage Matrix

| Paradigm | Route Prefix | Primary Tech Stack | Best Used For (When to Use) | Avoid For (When NOT to Use) |
| :--- | :--- | :--- | :--- | :--- |
| **Landing Page / Home Page** | `GET /` | HTML5 / CSS3 / Glassmorphism | Sub-millisecond landing page, documentation hubs, zero-latency initial entries. | Dynamic stateful pages requiring database binding or auth. |
| **MVC Portal** | `GET /portal` | Razor Views + Petite-Vue | SEO-critical public web portals, e-commerce, transactional form workflows. | Desktop-grade SPAs requiring continuous fluid UI transitions. |
| **Vue 2 SPA** | `GET /app` | ES Modules + Vuetify + Vuex | High-traffic, high-concurrency client dashboards with zero node build overhead. | Content-heavy pages dependent on public web search crawlers. |
| **Blazor Server** | `GET /blazor` | C# + MudBlazor + SignalR | Internal backoffice applications, admin tools, and intranet SPAs using full C# logic. | Public sites with tens of thousands of concurrent WebSocket circuits. |
| **Piranha CMS** | `GET /blogs`, `/articles` | Piranha CMS v12 + SQLite | Editorial blogs, news releases, tech articles, and content managed by non-devs. | Custom transactional business forms or operational APIs. |
| **Admin & APIs** | `GET /manager`, `/api` | REST Controllers + JWT + Scalar | Site content administration, mobile client backends, and programmatic integrations. | General end-user web application browsing. |

---

## 🚀 Architectural & Engineering Highlights

### 1. Dynamic Route & Options Configuration (BlazorSettings & MvcSettings)
*   **Configurable Blazor Prefix & Branding:** Blazor Server is configured via `appsettings.json` (`BlazorSettings:RoutePrefix` and `BlazorSettings:AppName`). Changing `"blazor"` to `"app2"`, `"portal"`, etc. dynamically updates route paths, challenge redirects, and layout headers.
*   **Configurable MVC Portal & Application Metadata:** Razor MVC is configured via `appsettings.json` (`MvcSettings:HomeRoute`, `MvcSettings:AppName`, and `MvcSettings:AppDescription`). Setting `HomeRoute` (e.g. `"portal"`) dynamically maps the portal route to `HomeController.Index()`, while `MvcOptions` injects `AppName` and `AppDescription` into layout titles and OpenGraph meta tags.
*   **Middleware Pipeline Sequencing:** Sub-path rewriting runs **before** `app.UseStaticFiles()`, allowing static web assets (like `_content/MudBlazor/...`) under sub-paths to return `HTTP 200 OK`.
*   **Root Protection:** `app.UseDefaultFiles()` is scoped via `app.UseWhen(ctx => string.IsNullOrEmpty(ctx.Request.PathBase))` so `wwwroot/index.html` is served exclusively on root domain requests (`/`) without intercepting Blazor sub-paths (`/app2`).

### 2. Unified Hybrid Authentication & Circuit State Preservation
*   **Unified Cookie Scheme:** Shared Cookie authentication (`.AspNetCore.Cookies` configured with `options.Cookie.Path = "/"`) grants single sign-on across MVC Razor views (`/portal` or `/home`) and Blazor Server (`/blazor`).
*   **SignalR Circuit Auth Preservation:** `ServerCookieAuthService` captures the HTTP GET `HttpContext.User` during initial connection and preserves the `ClaimsPrincipal` across SignalR WebSocket DI circuit scopes where `IHttpContextAccessor` is null.
*   **Static SSR Auth Routes:** Blazor `/login` and `/logout` pages render in Static SSR mode (`RenderMode = null`) to ensure HTTP `Set-Cookie` headers are properly dispatched to browsers.
*   **Dual Authentication Pipeline:** Controller APIs (`/api/...`) use JWT Bearer Security (`[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`) with refresh token revocation.

### 3. PostgreSQL Database Engine (Npgsql / EF Core) & SQLite Engine
*   **Relational Database Engine:** Application relational data uses `Npgsql.EntityFrameworkCore.PostgreSQL` targeting PostgreSQL with custom schema support (`net10multiparadigm`).
*   **Piranha CMS Engine:** Editorial data uses `Piranha.Data.EF.SQLite` targeting `App_Data/piranha.db`.
*   **High-Speed ADO.NET Bulk Seeding:** Inserts 10,000+ Billboard song records using raw parameterized ADO.NET SQL commands inside a single transaction, bypassing EF Core change-tracking overhead.

### 4. Interactive API Documentation & HTML-First Views
*   **Scalar OpenAPI Playground:** Visual API playground served at `/scalar/v1` (with `/swagger` redirected automatically).
*   **CDN-First Delivery:** Front-end libraries (Bootstrap 4, Font Awesome, MudBlazor, Petite-Vue, Vue 2, Vuetify) are served directly via **jsDelivr CDN**.

### 5. Multi-Paradigm Real-Time SignalR Messaging (`/chathub`)
*   **Shared Real-Time Infrastructure:** Central ASP.NET Core SignalR `ChatHub` at `/chathub` defined in `Services/Notifications/ChatHub.cs`.
*   **Cross-Paradigm Synchronization:** Demonstrates real-time direct messaging across 3 frontend paradigms simultaneously:
    *   **Razor MVC Portal (`/portal/chat`)**: Instagram/X DM-style UI built with HTML5 + Bootstrap 4, powered by `petite-vue` reactivity and `@microsoft/signalr` JS.
    *   **Razor MVC (`/home/chat`)**: Instagram/X DM-style UI built with HTML5 + Bootstrap 4, powered by `petite-vue` reactivity and `@microsoft/signalr` JS.
    *   **Vue 2.x SPA (`/app/#/chat`)**: Vuetify-based DM interface in `chat.vue.js` using `@microsoft/signalr` JS.
    *   **Blazor Server (`/blazor/chat`)**: MudBlazor interactive component in `Chat.razor` using C# `Microsoft.AspNetCore.SignalR.Client` `HubConnection`.
*   **In-Memory Live Transmitting:** Demonstrates high-speed WebSocket broadcasting without database locking or mandatory user logins.

### 6. MVC-Exclusive User Management Portal (`/Account/Users`)
*   **Interactive Petite-Vue UI:** Admin-only user management portal (`/Account/Users`) built with HTML5, Bootstrap 4, and `petite-vue` reactivity. Features 1-click role switcher pills, stat cards, instant search/filtering, and modal dialogs for user creation, role editing, and password resets.
*   **Fine-Grained Role & Piranha CMS Permission Mapping:** Supports `admin`, `CmsEditor`, `CmsWriter`, `CmsModerator`, and `user` roles. Dynamically maps role assignments to fine-grained Piranha CMS permissions (`PagesEdit`, `PostsEdit`, `MediaDelete`, `CommentsApprove`, etc.) via `UserAccountService.AddPiranhaRoleClaims()`.
*   **Strict Secondary Server Protection:** Enforces secondary server-side security checks on `UserAccountService` and `AccountController` to protect the primary seeded admin (`UserAccount.DEFAULT_ADMIN_LOGIN`):
    *   **Role Immunitization:** Prevents removing the `admin` role from the primary seeded admin.
    *   **Permanent Active Status:** Blocks deactivation of the primary admin account.
    *   **Deletion Protection:** Blocks deletion of the primary admin account.
    *   **Reserved Username:** Prevents creating new accounts using the reserved default admin username.

---

## 🚀 Getting Up & Running

### 1. PostgreSQL Database Setup & Connection Configuration

The application uses **PostgreSQL** (`Npgsql.EntityFrameworkCore.PostgreSQL`) as its primary relational database engine. All application tables are isolated under a custom database schema (`net10multiparadigm`).

1. **Configure Connection Details** in `Dotnet10MvcApi/appsettings.json`:
   ```json
   {
     "DatabaseProvider": "PostgreSQL",
     "DatabaseSchema": "",
     "ConnectionStrings": {
       "PostgreSqlConnection": "Host=192.168.0.200;Port=5433;Database=mypgDb;Username=pguser;Password=pgpassword"
     }
   }
   ```

2. **Automatic Migration & Data Seeding on Startup**:
   When launched under `Development` mode (`ASPNETCORE_ENVIRONMENT=Development`), the application automatically handles database initialization on startup:
   - **Schema & Table Migration:** Executes `db.Database.Migrate()` to ensure schema `net10multiparadigm` and all entity tables (`Products`, `Songs`, `UserAccounts`, `RefreshTokens`, `BlazorNotifications`) exist on your PostgreSQL server.
   - **Admin Account Auto-Seeding:** Ensures default administrator account (`admin` / `admin`) exists with Piranha CMS access claims.
   - **Products Dataset:** Seeds catalog product entries if empty.
   - **Billboard Songs Ingestion:** Uses high-speed raw ADO.NET bulk transactions to ingest 10,000+ Billboard song entries into `net10multiparadigm."Songs"`.
   - **Piranha CMS Engine:** Auto-seeds editorial blog posts and technical articles into `App_Data/piranha.db` (SQLite).

3. **Manual EF Core CLI Commands (Optional)**:
   If you prefer running EF Core CLI commands manually or creating new schema migrations:
   - **Apply Pending Migrations:**
     ```powershell
     dotnet ef database update --project Dotnet10MvcApi --context ApplicationDbContext
     ```
   - **Add New Migration:**
     ```powershell
     dotnet ef migrations add <MigrationName> --project Dotnet10MvcApi --context ApplicationDbContext
     ```

---

### 2. Launching the Application

Set `ASPNETCORE_ENVIRONMENT=Development` and run the application:

* **PowerShell**:
  ```powershell
  $env:ASPNETCORE_ENVIRONMENT="Development"
  dotnet run --project Dotnet10MvcApi
  ```
* **Command Prompt (CMD)**:
  ```cmd
  set ASPNETCORE_ENVIRONMENT=Development
  dotnet run --project Dotnet10MvcApi
  ```
* **One-Click Batch Script (`run-debug.bat`)**:
  ```powershell
  .\Dotnet10MvcApi\run-debug.bat
  ```

---

### 3. Application Endpoints & Access Guide

Once the application starts, access the 6 web application paradigms at the following URLs:

| Application Paradigm | Route | Description / Access Credentials |
| :--- | :--- | :--- |
| **Static Landing / Home Page** | `https://localhost:7031/` | Static Web Root landing page (`wwwroot/index.html`). |
| **Vue 2.x SPA (`/app`)** | `https://localhost:7031/app` | Zero-build, native ES module Single Page Application. |
| **Razor MVC Portal (`/portal`)** | `https://localhost:7031/portal` | Server-rendered Razor views with `petite-vue` reactivity. |
| **MVC User Management** | `https://localhost:7031/Account/Users` | User administration portal (MVC exclusive, Admin only). |
| **Blazor Server (`/blazor`)** | `https://localhost:7031/blazor` | Interactive Blazor Server with MudBlazor components. |
| **Piranha CMS Public** | `https://localhost:7031/blogs` | Editorial blog posts & technical articles (`/articles`). |
| **Piranha CMS Manager** | `https://localhost:7031/manager` | Admin management portal (**Login:** `admin` / `admin`). |
| **REST Web API Scalar Docs** | `https://localhost:7031/scalar/v1` | Interactive OpenAPI Scalar playground & `/swagger` redirect. |

#### Default Development Credentials
On initial startup, EF Core migrations automatically create database tables under schema `net10multiparadigm` and seed the default administrator account:
* **Administrator Account**:
  * **Username:** `admin`
  * **Password:** `admin`
  * **Role:** `admin`
* **Piranha CMS Admin**:
  * **Admin Portal (`/manager`):** `admin` / `admin`

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
│   │   ├── Pages/                       # Blazor Pages (Home, Auth, Weather, Counter, Products, Chat)
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
│   │       └── HomeController.cs        # MVC Home Portal (Index, About, Chat)
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
│   │   ├── Notifications/
│   │   │   ├── ChatHub.cs               # Cross-Paradigm SignalR Chat Hub (/chathub)
│   │   │   ├── NotificationHub.cs       # SignalR Notification Hub (/notificationhub)
│   │   │   └── NotificationService.cs   # Shared Notification Service
│   │   └── TokenManager.cs              # JWT Security Service
│   ├── Views/                           # MVC & Piranha CMS Razor View Templates
│   │   └── Home/
│   │       └── Chat.cshtml              # Petite-Vue + SignalR MVC Chat View
│   ├── wwwroot/                         # Public Static Web Root
│   │   ├── app/                         # Vue 2.x SPA (Zero-Build ES Modules)
│   │   │   └── src/pages/chat.vue.js   # Vuetify SignalR SPA Chat Page
│   │   ├── js/                          # Custom JS Utilities (geo.js, scroll.js)
│   │   └── index.html                   # Multi-Paradigm Glassmorphic Landing Portal
│   ├── Program.cs                       # Middleware Pipeline & Service Registration
│   ├── run-debug.bat                    # x64 Architecture Launcher Script
│   └── appsettings.json                 # Connection Strings, MvcSettings & BlazorSettings
└── README.md                            # Documentation
```

---

## 🏁 Getting Started

### Prerequisites
*   **.NET 10.0 SDK**
*   **PostgreSQL Database Server** (v14+)

### Build the Application
```powershell
dotnet build Dotnet10MvcApi/Dotnet10MvcApi.csproj
```

### Run the Application
```powershell
.\Dotnet10MvcApi\run-debug.bat
```

### Key Endpoints
*   **Static Landing / Home Page:** [https://localhost:7031/](https://localhost:7031/)
*   **ASP.NET Core MVC Portal:** [https://localhost:7031/portal](https://localhost:7031/portal) (or configured `MvcSettings:HomeRoute`)
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
