# Workspace Rules - SharpDevelopMVC Modernization

These instructions govern all future modifications, tests, and task executions performed by AI agents in this repository.

---

## 💻 Environment & Run Requirements

*   **x64 Emulation Constraint:** This application runs on Windows ARM64 but connects to an MS Access database via OLE DB drivers, which are exclusively compiled for x64/x86 architectures.
    *   **Rule:** Always run, debug, or build the project using the x64 architecture flag:
        ```powershell
        dotnet run --arch x64
        ```
    *   **Failure Mode:** Running without `--arch x64` results in `assembly not found` or `provider not registered` exceptions during database connection handshakes.
*   **Debug & Helper Script (`run-debug.bat`):** The project includes `Dotnet10MvcApi/run-debug.bat` to launch the application under the correct architecture:
    *   **Standard Run:** `.\Dotnet10MvcApi\run-debug.bat`
    *   **Agent Run (Low Verbosity):** `.\Dotnet10MvcApi\run-debug.bat --agent` (or `/agent`), which executes `dotnet run --project . --arch x64 --verbosity quiet`.

---

## 🗄️ Database & Queries (MS Access Jet / EF Core)

*   **Database Provider:** The project uses `EntityFrameworkCore.Jet` for database connections. Maintain compatibility for easy future shifts to **PostgreSQL**. Do not use MS SQL Server.
*   **Scalar Queries (#Dual):** The Jet provider translates LINQ evaluations like `.Any()` into SQL containing `FROM #Dual`. 
    *   **Rule:** The database must contain a helper table named `[#Dual]` with exactly one row. This table is automatically checked and seeded on startup in `Program.cs`. Do not delete or alter this table.
*   **Bulk Ingest Seeding:** Row-by-row EF Core change-tracked inserts for thousands of records are too slow for the Jet database engine.
    *   **Rule:** Seeding of large lists (like the Billboard songs database) must be executed using raw parameterized ADO.NET commands inside a single transaction (refer to `Song.Seed(...)`).

---

## 🔐 Hybrid Authentication Model

*   **Dual Authentication Schemas:** The project registers both Cookie and JWT Bearer schemes in `Program.cs`. The default scheme is Cookies.
    *   **MVC Pages:** Use standard `[Authorize]` attributes (which default to redirection to `/Account/Login`).
    *   **Web APIs:** Must explicitly request JWT Bearer authentication to check header authorizations:
        ```csharp
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        ```

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

*   **Markup Style:** Prefer standard HTML5 markup over legacy ASP.NET MVC Razor helpers (e.g., `@Html.BeginForm`, `@Html.TextBoxFor`, `@Html.LabelFor`).
    *   **Rule:** Implement views using clean, raw HTML form controls and Bootstrap 4 classes (`<form action="..." method="...">`, `<input id="..." name="..." class="form-control" />`). Use Razor syntax for essential dynamic control flow (loops, conditionals) and model properties rather than HTML helper abstractions.

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

---

## 🏗️ Multi-Paradigm Single-Project Architecture

*   **Unified Monolith Design:** This project is a single-project "one-stop shop" that hosts 4 web paradigms simultaneously out of a single ASP.NET Core process:
    1.  `/` -> Static Web Root Landing Page (`wwwroot/index.html`).
    2.  `/app` -> Vue 2.x Single Page Application (`wwwroot/app/index.html` via ES Modules).
    3.  `/home` -> Server-Rendered Razor MVC Views (`HomeController`).
    4.  `/api/...` -> Controller-based REST APIs with JWT Bearer security.
    *   **Rule:** Maintain this unified monolith design. Do not split these paradigms into separate projects or introduce node/webpack build servers.

*   **User Terminology & Domain Concepts:** When the user refers to the following terms, map them strictly to their corresponding application paradigm:
    *   **Landing Page:** The static web root landing page (`/` or `wwwroot/index.html`).
    *   **SPA** or **app:** The Vue 2.x Single Page Application (`/app` or `wwwroot/app/index.html`), a zero-node, zero-build app using native ES modules.
    *   **MVC** or **SSR:** ASP.NET Core MVC Razor server-side rendered views (`/home`), with lightweight client-side reactivity powered by `petite-vue`.
    *   **API** or **WebAPI:** Controller-based REST Web API endpoints (`/api/...`) with JWT Bearer authentication.

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
- **NO Package Installations:** Do **NOT** run command execution tools to perform node package installation (`npm install`, `npm i`, `yarn install`, `pnpm install`, etc.). The user manages dependencies manually.
