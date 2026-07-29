# Vue 2 SPA (Multi-Paradigm ASP.NET Core Monolith)

**Zero-Node, Zero-Build Vue 2 Single-Page Application**

This single-page application (SPA) built with Vue 2 and Vuetify is an integrated component of the multi-paradigm ASP.NET Core monolith (`Dotnet10MvcApi`), served at the `/app` route (`wwwroot/app`). It operates strictly on **native ES modules** and browser-executable code, maintaining our core philosophy of **No Node, No Build** for production runtime.

---

## 🏛️ Architecture & Monolith Context

This SPA is hosted within a single ASP.NET Core web process that seamlessly integrates five web paradigms:

1. **`/`** – Static Web Root Landing Page (`wwwroot/index.html`).
2. **`/app`** – **Vue 2 SPA (This App)** – Zero-build, native ES module Single-Page Application.
3. **`/home`** – Server-Rendered Razor MVC Views with `petite-vue` reactivity.
4. **`/api`** – Controller-based REST Web APIs with JWT Bearer security.
5. **`/blogs`, `/articles`, `/manager`** – Piranha CMS v12 content engine and admin management portal.

---

## ⚡ High-Concurrency & Mass Simultaneous User Optimization

This Vue 2 SPA architecture is engineered specifically to handle **mass simultaneous users** efficiently:

- **Zero Server-Side CPU Overhead:** Unlike Server-Side Rendering (SSR) frameworks (Nuxt, Next.js) which consume significant server CPU and RAM per concurrent request rendering HTML strings, this SPA shifts 100% of UI rendering, DOM updates, and client state management to the user's browser/GPU.
- **Ultra-Lean ASP.NET Core Static Serving:** ASP.NET Core serves the static SPA files (`/app/...`) via high-performance `UseStaticFiles()` middleware. When combined with HTTP/2 parallel streams and browser caching, server resource consumption per connection is negligible.
- **Stateless API Backend Scaling:** The SPA interacts with ASP.NET Core REST APIs (`/api/...`) via stateless JWT Bearer authorization header tokens. This decouples the client state from the server, allowing backend API nodes to scale horizontally without session stickiness.
- **Low Payload & Dynamic Module Preloading:** Native ES modules load asynchronously. Critical entry points are parallel-fetched via `<link rel="modulepreload">` tags to eliminate waterfall latency, delivering high responsiveness even for thousands of simultaneous clients.

---

## 🛠️ Development & Debugging Workflow

### ASP.NET Core Host Process (No Live Server Needed)
Because the SPA is served directly by the ASP.NET Core process, **VSCode's Live Server extension is NOT needed**.

Launch the monolith process locally using x64 architecture (required for MS Access OLE DB drivers):

```powershell
# Using the debug helper script:
.\Dotnet10MvcApi\run-debug.bat

# Or directly via dotnet CLI:
dotnet run --project .\Dotnet10MvcApi --arch x64
```

Access the SPA in your browser at `http://localhost:5000/app` (or `https://localhost:5001/app`).

### Optional Vite Integration for Agentic AI Debugging (Dev-Only, No Build)
Vite can optionally be leveraged as a dev-only debugging server specifically tailored for **Agentic AI coding assistants (such as Antigravity)** and browser subagents to rapidly find and fix UI bugs:
- **Agentic AI Debugger Workflow:** Provides AI subagents with instant Hot Module Replacement (HMR), rich console error overlays, and precise source-mapped stack traces. This enables AI agents to quietly inspect, diagnose, and verify frontend changes in seconds without needing full application restarts or manual browser reloads.
- **Port & Backend API Proxying:** Runs locally on `http://localhost:5173`. Pre-configured in `vite.config.js` to automatically proxy all `/api` requests to the running ASP.NET Core host process at `http://localhost:5000`.
- **Strict No-Build Rule:** Vite is **never** used to bundle or compile production assets. Production runtime deployment remains 100% native ES modules served directly by ASP.NET Core from `wwwroot/app`.
- **CDN Script Toggle (Dev vs. Production):** Production `index.html` uses minified CDN scripts (`vue.min.js`, `vuetify.min.js`). During active AI debugging sessions, AI agents may temporarily switch script tags to unminified dev variants (`vue.js`, `vuetify.js`) for verbose Vue warning messages and DevTools inspection, restoring `.min.js` variants before completing work.
- **Clean Workspace Strategy (`node_modules`):** If Node tools or Vite are used for AI debugging, `node_modules` must remain strictly `.gitignore`d or installed globally / outside the repository directory. This keeps the codebase clean, lightweight, and free of repository dependency bloat.

### Recommended VSCode Extensions
- [Template Literal Editor](https://marketplace.visualstudio.com/items?itemName=plievone.vscode-template-literal-editor)
- [Comment tagged templates](https://marketplace.visualstudio.com/items?itemName=bierner.comment-tagged-templates)
- [Vue.js devtools (Browser Extension)](https://chromewebstore.google.com/detail/vuejs-devtools/iaajmlceplecbljialhhkmedjlpdblhp)
- [Trailing Spaces](https://marketplace.visualstudio.com/items?itemName=shardulm94.trailing-spaces)

*(Note: Live Server extension is omitted as serving is handled natively by the .NET process.)*

---

## 💡 Why Avoid Heavy Build Pipelines (CLI, npm, Webpack)?

1. **Zero Dependency Maintenance & Rot:** Eliminates constant `npm audit` vulnerabilities, lockfile conflicts, and breaking bundler upgrades.
2. **Instant Developer Feedback:** Edits to `.js` files immediately reflect upon browser refresh without compilation wait times.
3. **CDN-First Asset Delivery:** Core UI dependencies (Vuetify, Vue 2, Material Design Icons) are fetched from high-speed public CDNs (`cdn.jsdelivr.net`) or cached locally in `wwwroot`.
4. **Native Browser Standard Execution:** Modern browsers natively support ES modules (`import`/`export`), rendering transpilation steps redundant.

---

## 🌊 Mitigating Waterfall Module Loading

To prevent waterfall network loading inherent to unbundled ES module trees (`main.js` -> `app.js` -> `components`):
- **`modulepreload` Declarations:** `<link rel="modulepreload">` tags are embedded in `index.html` to direct the browser to fetch dependent module scripts in parallel during initial layout parse.
- **HTTP/2 Multiplexing:** ASP.NET Core and CDN edge servers multiplex module requests over single TCP streams, minimizing latency penalties.

---

## 👤 Maintainer

- **Bernard Gabon** ([bernardgabon.com](https://bernardgabon.com))
