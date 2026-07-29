# Workspace Rules - SharpDevelopMVC Modernization

These instructions govern all future modifications, tests, and task executions performed by AI agents in this repository.

---

## 💻 Environment & Run Requirements

*   **Development Environment Configuration:**
    *   **Rule:** Always silently ensure `ASPNETCORE_ENVIRONMENT` is set to `Development` prior to running or debugging the application:
        ```powershell
        $env:ASPNETCORE_ENVIRONMENT="Development"
        dotnet run --project Dotnet10MvcApi --arch x64
        ```
    *   **Rationale:** Running under `Development` ensures `appsettings.Development.json` is loaded, providing development JWT secret keys, fallback DevUser accounts (`devadmin`/`admin123`), and enabling Piranha CMS auto-seeding without prompting the user.
*   **x64 Emulation Constraint:** This application runs on Windows ARM64 but connects to an MS Access database via OLE DB drivers, which are exclusively compiled for x64/x86 architectures.
    *   **Rule:** Always run, debug, or build the project using the x64 architecture flag:
        ```powershell
        dotnet run --arch x64
        ```
    *   **Failure Mode:** Running without `--arch x64` results in `assembly not found` or `provider not registered` exceptions during database connection handshakes.
*   **Debug & Helper Script (`run-debug.bat`):** The project includes `Dotnet10MvcApi/run-debug.bat` to silently launch the application under Development mode with correct architecture:
    *   **Standard Run:** `.\Dotnet10MvcApi\run-debug.bat`
    *   **Agent Run (Low Verbosity):** `.\Dotnet10MvcApi\run-debug.bat --agent` (or `/agent`), which automatically sets `ASPNETCORE_ENVIRONMENT=Development` and executes `dotnet run --project . --arch x64 --verbosity quiet`.
*   **App Execution & Clickable URLs Rule:**
    *   **Paradigm-Specific Clickable Links:** Whenever the application has been debugged and launched successfully, include clickable Markdown links strictly for the main URL of the specific paradigm currently being worked on or debugged (e.g. [https://localhost:7031/app](https://localhost:7031/app) for Vue SPA, [https://localhost:7031/home](https://localhost:7031/home) for Razor MVC, [https://localhost:7031/manager](https://localhost:7031/manager) for Piranha CMS, or [https://localhost:7031/scalar/v1](https://localhost:7031/scalar/v1) for REST APIs), rather than listing sub-pages or all application URLs indiscriminately.
    *   **No Unsolicited Launching:** If you are not actively debugging a runtime issue, do **not** launch the application right away. Instead, offer to run/launch the app for the user and ask for their confirmation first.

---

## 🗄️ Database & Queries (MS Access Jet / SQLite / EF Core)

*   **Database Providers:** 
    *   Application relational data uses `EntityFrameworkCore.Jet` targeting MS Access (`MyAccessDb.mdb`). Maintain compatibility for easy future shifts to **PostgreSQL**. Do not use MS SQL Server.
    *   Piranha CMS content data uses `Piranha.Data.EF.SQLite` targeting `App_Data/piranha.db`.
*   **Scalar Queries (#Dual):** The Jet provider translates LINQ evaluations like `.Any()` into SQL containing `FROM #Dual`. 
    *   **Rule:** The database must contain a helper table named `[#Dual]` with exactly one row. This table is automatically checked and seeded on startup in `Program.cs`. Do not delete or alter this table.
*   **Bulk Ingest Seeding:** Row-by-row EF Core change-tracked inserts for thousands of records are too slow for the Jet database engine.
    *   **Rule:** Seeding of large lists (like the Billboard songs database) must be executed using raw parameterized ADO.NET commands inside a single transaction (refer to `Song.Seed(...)`).

---

## 🔐 Hybrid Authentication & Piranha CMS Security

*   **Dual Authentication Schemas:** The project registers both Cookie and JWT Bearer schemes in `Program.cs`. The default scheme is Cookies.
    *   **MVC Pages:** Use standard `[Authorize]` attributes (which default to redirection to `/Account/Login`).
    *   **Web APIs:** Must explicitly request JWT Bearer authentication to check header authorizations:
        ```csharp
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        ```
    *   **Piranha CMS Manager Portal:** Accessible at `/manager`. Administrators (`admin`) logging in are automatically granted Piranha Manager security claims (`Piranha.Manager.Permission.All()`).
*   **Piranha Manager Anti-Forgery Token Handling:**
    *   **Current Setup:** Anti-Forgery validation for Piranha Manager API routes (`/manager/api/*`) is temporarily bypassed using `BypassManagerAntiforgery.cs` registered in `Program.cs`.
    *   **Future Restoration & Fix Guide:** To re-enable strict Anti-Forgery validation on `/manager/api`:
        1. Remove the `BypassManagerAntiforgery` DI decorator registration in `Program.cs`.
        2. Ensure `builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN")` is set without overriding `options.Cookie.Name` (keeping ASP.NET Core's internal `CookieToken` separate).
        3. Ensure the middleware appends `tokens.RequestToken` into a non-`HttpOnly` cookie named `"XSRF-TOKEN"` on `/manager` GET requests so Piranha Manager's Vue frontend JS populates `x-xsrf-token` headers on `POST /manager/api/post/save` requests.

---

## 📷 GDI+ & Image Processing

*   **Windows Target Platform:** GDI+ library calls (`System.Drawing.Common`) are used for image rotation, scaling, and thumbnail rendering in `ImageUploadExtension.cs`.
    *   **Rule:** Platform compatibility warning `CA1416` (not supported on non-Windows platforms) can be ignored or suppressed, as the project is platform-locked to Windows due to OLE DB driver constraints.

---

## 📄 API Documentation

*   **API Reference Route:** The OpenAPI docs and visual playground are powered by **Scalar** (served at `/scalar/v1`).
    *   **Rule:** Ensure that the legacy redirection endpoint `/swagger` in `Program.cs` remains mapped to `/scalar/v1` for convenience.

---

## 🌐 HTML-First View Implementation

* **Markup Style:** Prefer standard HTML5 markup over legacy ASP.NET MVC Razor helpers (e.g., `@Html.BeginForm`, `@Html.TextBoxFor`, `@Html.LabelFor`).
  * **Rule:** Implement views using clean, raw HTML form controls and Bootstrap 4 classes (`<form action="..." method="...">`, `<input id="..." name="..." class="form-control" />`). Use Razor syntax for essential dynamic control flow (loops, conditionals) and model properties rather than HTML helper abstractions.

* **Razor Comments over HTML Comments:** All developer annotations in `.cshtml` and `.razor` view/component files must use Razor comment syntax, **not** HTML comment syntax.
  * **Rule:** Use `@* comment text *@` for all comments inside Razor views and components (`.cshtml` and `.razor`). Never use `<!-- comment -->` for developer notes or structural annotations in `.cshtml` or `.razor` files.
  * **Rationale:** HTML comments (`<!-- -->`) are emitted into the HTTP response payload and are visible to end users via browser DevTools or View Source. Razor comments (`@* *@`) are stripped server-side before the response is sent, keeping rendered HTML payloads clean, secure, and lean.

---

## ⚡ Client-Side Interactivity (Petite-Vue vs jQuery)

*   **Petite-Vue Preference:** Use `petite-vue` (`https://cdn.jsdelivr.net/npm/petite-vue@0.4.1/dist/petite-vue.es.js`) for lightweight client-side reactivity, state management, form validation feedback, and dynamic toggle behaviors.
    *   **Rule:** Avoid writing new jQuery DOM manipulation or event listener code. Prefer `petite-vue` reactive state blocks (`createApp({ ... }).mount('#elementId')`) for all tiny and minor view interactivity. Remember to escape event directive syntax in Razor `.cshtml` files using `@@click`, `@@submit`, etc.

---

## 🚀 CDN-First Front-End Asset Delivery

*   **jsDelivr Preference:** All third-party front-end libraries, CSS frameworks, JavaScript utilities, font packages, and icon sets must be served via public CDN, preferring **jsDelivr** (`cdn.jsdelivr.net`).
    *   **Rule:** Do not reference local vendor asset files (such as Bootstrap, jQuery, Bootbox, Font Awesome, jQuery Validation, jExcel/jSpreadsheet, jSuites, Petite-Vue, Pocket-Vue) in `wwwroot/lib` or `wwwroot/js`. Always link directly to official `cdn.jsdelivr.net` URLs.
    *   **Razor Escaping:** In Razor `.cshtml` files, remember to escape any `@` symbols in npm package CDN URLs (e.g., `https://cdn.jsdelivr.net/npm/@@fortawesome/fontawesome-free@5.15.4/css/all.min.css`).
    *   **Application Custom Styles:** Application-specific custom stylesheets (such as `Site.css` and `Account.css`) remain local in `wwwroot/css/`.
*   **CDN Script Minification (Development vs. Production):**
    *   **Production Standard:** `wwwroot/app/index.html` references minified CDN scripts (`vue.min.js`, `vuetify.min.js`, `vue-router.min.js`, `vuex.min.js`).
    *   **AI Debugging Mode:** During active debugging or troubleshooting session using Vite or browser subagents, AI agents may temporarily switch script tags to unminified dev variants (`vue.js`, `vuetify.js`, etc.) to obtain verbose console warnings and full Vue DevTools hook support. Once debugging is complete, the AI agent must ensure script tags are restored to their production `.min.js` variants before finalizing tasks.

---

## 🏗️ Multi-Paradigm Single-Project Architecture

*   **Unified Monolith Design:** This project is a single-project "one-stop shop" that hosts multi-paradigm web experiences simultaneously out of a single ASP.NET Core process, now integrated with **Piranha CMS**:
    1.  `/` -> Static Web Root Landing Page (`wwwroot/index.html`).
    2.  `/app` -> Vue 2.x Single Page Application (`wwwroot/app/index.html` via ES Modules).
    3.  `/home` -> Server-Rendered Razor MVC Views (`HomeController`).
    4.  `/api/...` -> Controller-based REST APIs with JWT Bearer security.
    5.  `/blogs`, `/articles`, `/manager` -> **Piranha CMS v12**: Public editorial content engine for blogs, technical articles, custom block types (`HeroBlock`), and full headless/SSR admin management portal (`/manager`).
    *   **Rule:** Maintain this unified monolith design. Do not split these paradigms into separate projects or introduce node/webpack build servers.

*   **User Terminology & Domain Concepts:** When the user refers to the following terms, map them strictly to their corresponding application paradigm:
    *   **Landing Page:** The static web root landing page (`/` or `wwwroot/index.html`).
    *   **SPA** or **app:** The Vue 2.x Single Page Application (`/app` or `wwwroot/app/index.html`), a zero-node, zero-build app using native ES modules.
    *   **MVC** or **SSR:** ASP.NET Core MVC Razor server-side rendered views (`/home`), with lightweight client-side reactivity powered by `petite-vue`.
    *   **API** or **WebAPI:** Controller-based REST Web API endpoints (`/api/...`) with JWT Bearer authentication.
    *   **CMS** or **Piranha**: Piranha CMS v12 content engine serving public blogs (`/blogs`), technical articles (`/articles`), custom blocks (`HeroBlock`), and the admin manager portal (`/manager`).

*   **Feature Architecture Analysis & Paradigm Selection Rule:**
    *   Whenever the user / System Architect requests to add or implement a new feature in this multi-paradigm monolith:
        1. **Analyze Requirements & Recommend Paradigm:** The AI agent must carefully analyze the architectural requirements against the project's paradigms:
           - **Static Web Root (`/`)**: Static landing pages or marketing content.
           - **Vue 2 SPA (`/app`)**: Dynamic, zero-build single-page applications for high-concurrency client-side app workflows.
           - **Razor MVC (`/home`)**: Server-rendered HTML forms, traditional page-based workflows, or admin views with `petite-vue` reactivity.
           - **REST Web APIs (`/api/...`)**: Controller-based JSON endpoints with JWT Bearer authentication.
           - **Piranha CMS (`/blogs`, `/articles`, `/manager`)**: Editorial blog posts, articles, CMS block content, and admin management.
        2. **Mindful Architectural Discussion:** Discuss the proposed paradigm recommendation mindfully with the System Architect before proceeding with code implementation.
        3. **Autonomous AI Decision-Making:** If the Architect is unsure, defers the choice, or has limited knowledge of the project's paradigms, the AI agent must make an informed, appropriate architectural decision autonomously and explain the rationale clearly before building.

*   **Virtual / Sub-Application Isolation Rule (Optional Pattern):**
    *   When intentionally scaling or adding multiple distinct sub-applications (especially within the same paradigm), follow the folder organization and layout isolation patterns detailed in [.agents/VIRTUAL_APPS_GUIDE.md](file:///.agents/VIRTUAL_APPS_GUIDE.md):
        - **Vue 2 SPA:** Duplicate SPA folder under `wwwroot/app2`, `wwwroot/app3` (leveraging dynamic Vue Router base detection in `router.js`).
        - **Razor MVC:** Named controllers (`App3HomeController`) + view folders (`Views/App3Home/`) + folder `_ViewStart.cshtml` for `_LayoutApp3.cshtml` (avoiding ASP.NET Core Areas).
        - **Blazor Server:** `Pages/App3/` folder convention matching `@page "/app3/*"` routes + `_Imports.razor` for `App3Layout.razor`.
    *   **Note:** This virtual isolation pattern is **optional** and only relevant when introducing multiple sub-applications. Routine feature development must default to existing core paths. Always respect reserved host routes (`/`, `/app`, `/blazor`, `/api`, `/blogs`, `/articles`, `/manager`, `/scalar`).

---

## 🤖 Agentic AI Debugging Tooling (Vue 2 SPA Vite Debugger)

*   **AI Debugging Dev Server:** The Vue 2 SPA (`Dotnet10MvcApi/wwwroot/app`) includes a lightweight, pre-configured Vite dev server (`package.json`, `vite.config.js`) tailored specifically for AI agents (Antigravity & browser subagents) to quietly test, inspect, and debug UI issues.
    *   **Port & Proxy Setup:** Runs on `http://localhost:5173`. API calls to `/api` are automatically proxied to the ASP.NET Core backend process at `http://localhost:5000`.
    *   **Strict No-Build Rule:** Vite is **strictly a dev-time debugger for AI agents**. Never run `npm run build` or generate static bundle outputs. Production runtime deployment remains 100% native ES modules served directly by ASP.NET Core from `wwwroot/app`.
    *   **Clean Workspace Rule:** Node dependencies (`node_modules`) must remain strictly `.gitignore`d or installed globally/outside the project workspace to prevent repository bloat.

---

## 🛠️ Execution & Operation Standards

### 1. Git & Shell Command Execution (Standardized)
- **High-Resource / Token-Cost Actions:** For operations with high token usage or interactive prompts (e.g., `git push`, `git fetch`, `git pull`, `git log` without limits, large `git diff`, or commands requiring network authentication), the AI must **NOT** execute them directly. Instead, ask for explicit user permission first or prompt the user with the exact command to run locally.
- **Medium to Low Resource / Token-Cost Actions:** For lightweight, local, and non-interactive operations (e.g., `git add`, `git commit` [unless heavy pre-commit hooks are active], unstaging files, simple `git status` checks, or local file updates), the AI is permitted to execute them directly to speed up workflows, provided no manual approval loops are triggered.

### 2. Code Inspection & Search (Standardized)
- **Precise File Views:** Do not read entire large files at once. Read targeted line ranges (using start/end parameters) to locate class definitions or functions.
- **Scoped Searching:** When searching the codebase via grep or ripgrep, scope the query to specific file paths or extensions (e.g., using `Includes` filters) rather than scanning the entire directory.

### 3. Build & Test Operations (Standardized)
- **Verbosity Constraints:** For successful/routine runs, use minimal verbosity to keep context clean.
- **Exception for Failures:** If a build or test fails, use standard or detailed verbosity to ensure the full error messages, stack traces, and compiler warnings are captured for accurate debugging.
- **NO Package Installations (Except Vite AI Debugging):** Do **NOT** run command execution tools to perform general node package installations (`npm install`, `npm i`, `yarn install`, `pnpm install`, etc.), as the user manages project dependencies manually. An exception is made strictly for Vite dev dependencies in `wwwroot/app` when required to initialize or run the Agentic AI Dev Debugger.
