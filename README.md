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

### 5. Multi-Paradigm Real-Time SignalR Messaging (`/chathub`)
*   **Shared Real-Time Infrastructure:** Central ASP.NET Core SignalR `ChatHub` at `/chathub` defined in `Services/Notifications/ChatHub.cs`.
*   **Cross-Paradigm Synchronization:** Demonstrates real-time direct messaging across 3 frontend paradigms simultaneously:
    *   **Razor MVC (`/home/chat`)**: Instagram/X DM-style UI built with HTML5 + Bootstrap 4, powered by `petite-vue` reactivity and `@microsoft/signalr` JS.
    *   **Vue 2.x SPA (`/app/#/chat`)**: Vuetify-based DM interface in `chat.vue.js` using `@microsoft/signalr` JS.
    *   **Blazor Server (`/blazor/chat`)**: MudBlazor interactive component in `Chat.razor` using C# `Microsoft.AspNetCore.SignalR.Client` `HubConnection`.
*   **In-Memory Live Transmitting:** Demonstrates high-speed WebSocket broadcasting without database locking or mandatory user logins.

---

## 🚀 Getting Up & Running

### 1. Environment & Architecture Constraints

To ensure configuration files (`appsettings.Development.json`), default development credentials, and MS Access database OLE DB drivers load properly, set the environment to **Development** and run under **x64**:

* **PowerShell**:
  ```powershell
  $env:ASPNETCORE_ENVIRONMENT="Development"
  dotnet run --project Dotnet10MvcApi --arch x64
  ```
* **Command Prompt (CMD)**:
  ```cmd
  set ASPNETCORE_ENVIRONMENT=Development
  dotnet run --project Dotnet10MvcApi --arch x64
  ```

> [!IMPORTANT]
> **x64 Emulation Constraint:** The MS Access Jet database engine requires 64-bit OLE DB drivers (`Microsoft.ACE.OLEDB`). Always specify `--arch x64` when running or building.

---

### 2. One-Click Launcher Script (`run-debug.bat`)

The repository includes a helper script `Dotnet10MvcApi/run-debug.bat` that automatically configures `ASPNETCORE_ENVIRONMENT=Development` and launches under `--arch x64`:

*   **Standard Interactive Run:**
    ```powershell
    .\Dotnet10MvcApi\run-debug.bat
    ```
*   **Low-Verbosity Agent Run:**
    ```powershell
    .\Dotnet10MvcApi\run-debug.bat --agent
    ```

---

### 3. Application Endpoints & Access Guide

Once the application starts, access the 6 web application paradigms at the following URLs:

| Application Paradigm | Route | Description / Access Credentials |
| :--- | :--- | :--- |
| **Static Glassmorphic Landing** | `https://localhost:7031/` | Static Web Root landing page (`wwwroot/index.html`). |
| **Vue 2.x SPA (`/app`)** | `https://localhost:7031/app` | Zero-build, native ES module Single Page Application. |
| **Vue 2.x Real-Time Chat** | `https://localhost:7031/app/#/chat` | Real-Time DM interface in Vue 2 SPA. |
| **Razor MVC (`/home`)** | `https://localhost:7031/home` | Server-rendered Razor views with `petite-vue` reactivity. |
| **Razor MVC Real-Time Chat** | `https://localhost:7031/home/chat` | Instagram DM-style real-time chat with `petite-vue` + SignalR. |
| **Blazor Server (`/blazor`)** | `https://localhost:7031/blazor` | Interactive Blazor Server with MudBlazor components. |
| **Blazor Real-Time Chat** | `https://localhost:7031/blazor/chat` | MudBlazor SignalR DM component. |
| **Piranha CMS Public** | `https://localhost:7031/blogs` | Editorial blog posts & technical articles (`/articles`). |
| **Piranha CMS Manager** | `https://localhost:7031/manager` | Admin management portal (**Login:** `admin` / `admin`). |
| **REST Web API Scalar Docs** | `https://localhost:7031/scalar/v1` | Interactive OpenAPI Scalar playground & `/swagger` redirect. |

#### Default Development Credentials
* **Database / MVC / API User Accounts**:
  * Administrator: `admin` / `admin`
  * Standard User: `user` / `user`
* **Piranha CMS Admin**:
  * Admin Portal (`/manager`): `admin` / `admin`

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
