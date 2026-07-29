# Virtual App Architecture Guide: Multi-Paradigm Isolation in ASP.NET Core

## 📌 Executive Summary

> [!NOTE]
> **Optional / Architectural Reference Only:** Following this virtual isolation pattern is **not required or necessary** for standard features or routine development. It is an architectural reference to keep in mind only when intentionally adding multiple distinct sub-applications—especially when scaling multiple apps within the same paradigm (e.g., adding a 2nd/3rd Vue SPA in `wwwroot/app2`, creating separate virtual MVC portals, or hosting multiple Blazor apps).

This architecture guide details the **Virtual/Fake-Isolated Multiple App Pattern** within a unified ASP.NET Core monolith. 

Instead of creating separate `.exe` server processes, complex ASP.NET Core Areas, or microservices, this pattern achieves full visual, layout, and routing isolation for multiple sub-applications ("virtual apps") using clean folder conventions and dedicated layout shells.

---

## 🏗️ Architecture Overview Across Paradigms

| Paradigm | Isolation Boundary | Disk Folder Convention | Route Matching | Dedicated Layout Shell |
| :--- | :--- | :--- | :--- | :--- |
| **Vue 2 SPA** | **Physical Static Root** | `wwwroot/app3/` | `/app3/*` | `wwwroot/app3/index.html` |
| **ASP.NET Core MVC** | **Virtual Controller/View** | `Views/App3Home/` | `/App3Home/*` or `[Route("app3/*")]` | `Views/Shared/_LayoutApp3.cshtml` |
| **Blazor Server** | **Virtual Component/Layout** | `Pages/App3/` | `@page "/app3/*"` | `Pages/App3/App3Layout.razor` |

---

## 1. Vue 2 SPA: Zero-Touch Copy-Paste Isolation

Each SPA lives in its own static folder inside `wwwroot/`. Because the Vue Router uses **dynamic location detection**, copy-pasting an existing SPA folder (such as `wwwroot/app`) to a new folder (e.g. `wwwroot/app3`) requires **zero code changes** inside the SPA JS code!

### Folder Structure
```text
wwwroot/
├── app/                  # Main Vue SPA (/app)
│   ├── index.html
│   └── src/router.js
├── app3/                 # Virtual Vue SPA 3 (/app3)
│   ├── index.html
│   └── src/router.js
└── app4/                 # Virtual Vue SPA 4 (/app4)
    ├── index.html
    └── src/router.js
```
---

## 2. ASP.NET Core MVC: Simplified Virtual Isolation (No Areas)

Avoids complex ASP.NET Core Area route registrations by relying on carefully named Controllers, conventional View folder structures, and folder-level `_ViewStart.cshtml` layout assignments.

### Folder Structure
```text
Controllers/
├── App3HomeController.cs       # Controller for App 3 Home
└── App3DashboardController.cs  # Controller for App 3 Dashboard

Views/
├── Shared/
│   ├── _Layout.cshtml          # Default global layout
│   ├── _LayoutApp3.cshtml      # Dedicated App 3 Layout (Theme, Navbar)
│   └── _LayoutApp4.cshtml      # Dedicated App 4 Layout
├── App3Home/
│   ├── _ViewStart.cshtml       # Sets Layout = "_LayoutApp3" for App3Home
│   ├── Index.cshtml
│   └── Details.cshtml
└── App3Dashboard/
    ├── _ViewStart.cshtml       # Sets Layout = "_LayoutApp3" for App3Dashboard
    └── Index.cshtml
```

### Automatic Layout Switcher (`Views/App3Home/_ViewStart.cshtml`)
```razor
@* Automatically applies _LayoutApp3 to all views in this folder *@
@{
    Layout = "_LayoutApp3";
}
```

### Controller Example (`Controllers/App3HomeController.cs`)
```csharp
namespace Dotnet10MvcApi.Controllers;

[Route("app3/[action]")]
public class App3HomeController : Controller
{
    // Resolves to URL: /app3/index -> View: Views/App3Home/Index.cshtml
    public IActionResult Index() => View();

    // Resolves to URL: /app3/details -> View: Views/App3Home/Details.cshtml
    public IActionResult Details() => View();
}
```

---

## 3. Blazor Server: Folder Conventions & Scoped Imports

Blazor Server components use folder-matching `@page` directives and directory-level `_Imports.razor` files to automatically assign dedicated layouts.

### Folder Structure
```text
Pages/
├── App3/
│   ├── _Imports.razor          # Applies App3Layout to all App3 components
│   ├── App3Layout.razor        # Master layout component for App 3
│   ├── Index.razor             # @page "/app3"
│   └── MyPage.razor            # @page "/app3/mypage"
└── App4/
    ├── _Imports.razor          # Applies App4Layout to all App4 components
    ├── App4Layout.razor        # Master layout component for App 4
    └── Dashboard.razor         # @page "/app4/dashboard"
```

### Directory Level Layout Inheritance (`Pages/App3/_Imports.razor`)
```razor
@* Automatically sets the layout for all Blazor pages under Pages/App3/ *@
@layout App3Layout
```

### Blazor Page Component (`Pages/App3/MyPage.razor`)
```razor
@page "/app3/mypage"

<h3>App 3 - My Page</h3>
<p>This component automatically uses App3Layout.razor!</p>
```

---

## ⚠️ Route Conflict Avoidance & System Reservations

To prevent virtual apps from colliding with existing monolith components, Piranha CMS, or ASP.NET Core internal routes, **never** name virtual app prefixes after the following reserved paths:

### 🚫 Reserved Host Routes
| Reserved Route Prefix | Owner Paradigm | Purpose |
| :--- | :--- | :--- |
| `/` | Web Root Landing Page | Static Landing Page (`wwwroot/index.html`) |
| `/app` | Default SPA | Primary Vue 2 Single Page App |
| `/blazor` | Blazor Server | SignalR interactive circuit host & endpoints |
| `/api/*` | REST Web APIs | REST API endpoints (JWT authentication) |
| `/blogs/*`, `/articles/*` | Piranha CMS | Public CMS blog & article pages |
| `/manager/*` | Piranha CMS | Admin CMS Manager Portal |
| `/scalar/*`, `/swagger` | OpenAPI Docs | Scalar API Playground & Swagger redirect |
| `/notificationhub` | SignalR Hub | Real-time notifications endpoint |

### ✅ Recommended Virtual App Naming Rules
* Use unique, prefixed names for virtual apps (e.g. `/app3`, `/app4`, `/shop-portal`, `/v-admin`, `/app3-dashboard`).
* For MVC controllers, prefix controller classes clearly (e.g., `App3HomeController`, `App3ProductsController`) to keep them segregated from default controllers (`HomeController`, `AccountController`, `CrudsampleController`).

---

## 🔐 Shared Services & Data Layer

Because all virtual apps run in the same ASP.NET Core process:
1. **Shared Authentication:** JWT Bearer and Cookie authentication are shared natively. Virtual apps can restrict access using `[Authorize(Roles = "App3User")]` or policy checks.
2. **Shared Database Access:** All virtual apps share EF Core DbContexts (`MyAccessDb` / SQLite CMS) and ADO.NET query extensions.
3. **Unified API Gateway:** Controllers under `/api/...` serve JSON data to Vue 2 SPAs, Razor MVC views (`petite-vue`), and Blazor components seamlessly.
