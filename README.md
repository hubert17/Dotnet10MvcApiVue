# Modernized .NET 10 MVC & Web API

This project is a modern port and upgrade of the legacy SharpDevelop MVC 4 application to **ASP.NET Core (.NET 10.0)**. It serves as a unified **Modern Layered Monolith** combining server-rendered Razor MVC views with secure JSON Web Token (JWT) REST APIs.

---

## 🚀 Key Features & Architecture

*   **Modern Framework (.NET 10.0):** Built on **ASP.NET Core 10.0** utilizing a clean, controller-based layered monolith architecture (`Controllers/Mvc` for Razor views and `Controllers/Api` for REST endpoints).
*   **MS Access Database Provider (Jet / EF Core):** Integrates `EntityFrameworkCore.Jet` targeting an MS Access database file (`MyAccessDb.mdb`). Structured for easy future migration to **PostgreSQL**.
    *   **`#Dual` Table Engine Support:** Automatically initializes and seeds the required single-row `[#Dual]` table on startup to support Jet SQL scalar evaluations (such as LINQ `.Any()`).
    *   **High-Speed Bulk Seeding:** Inserts 10,000+ Billboard dataset records in **under 2 seconds** using parameterized raw ADO.NET SQL commands executed in a single transaction (bypassing EF Core change-tracking overhead).
*   **Hybrid Dual Authentication Pipeline:**
    *   **Cookie Authentication (Default):** Secures traditional server-rendered Razor pages (`/Account/Login`, product management portals).
    *   **JWT Bearer Tokens:** Secures REST API controllers (`[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`). Includes token generation, validation, refresh tokens, and token revoking.
*   **Interactive API Console (Scalar):** Features **Scalar** OpenAPI visualizer served at `/scalar/v1` (with legacy `/swagger` routes automatically redirected to `/scalar/v1`).
*   **HTML-First Views & CDN-First Assets:**
    *   **HTML-First Views:** Standard HTML5 markup with Bootstrap 4 classes, using Razor syntax strictly for control flow and model values instead of legacy `@Html` helper abstractions.
    *   **CDN Asset Delivery:** All third-party front-end libraries (Bootstrap 4, Font Awesome, Bootbox, Petite-Vue) are loaded directly via **jsDelivr CDN** (`cdn.jsdelivr.net`).
    *   **Lightweight Reactivity:** Client-side interactivity and form feedback powered by **petite-vue**.
*   **Utilities & Services:**
    *   **Async Email Service:** Dispatch HTML emails with attachment handling via `IFormFile`.
    *   **Image Processing:** Automatic image scaling, EXIF orientation correction, and thumbnail generation using GDI+ (`System.Drawing.Common`).

---

## 🛠️ Windows-on-ARM64 & x64 Run Requirements

Because the application connects to an MS Access database via Microsoft OLE DB drivers (`Microsoft.ACE.OLEDB`), which are exclusively compiled for x86/x64 architectures:

> [!IMPORTANT]
> **x64 Emulation Constraint:** Always build, run, and debug the application specifying the `--arch x64` target flag. Running without `--arch x64` on ARM64 Windows will cause OLE DB provider registration errors.

### Helper Batch Script (`run-debug.bat`)
The project includes a launch script at `Dotnet10MvcApi/run-debug.bat`:
*   **Standard Interactive Run:**
    ```powershell
    .\Dotnet10MvcApi\run-debug.bat
    ```
*   **Low-Verbosity Agent Run:**
    ```powershell
    .\Dotnet10MvcApi\run-debug.bat --agent
    ```

---

## 📂 Project Structure

```text
Dotnet10MvcApi/
├── App_Data/
│   ├── MyAccessDb.mdb               # MS Access database file
│   └── BillboardTo2013.zip          # Billboard CSV dataset zip
├── Controllers/
│   ├── Api/                         # REST API Controllers (JSON & JWT Auth)
│   │   ├── AccountController.cs     # Auth endpoints (/TOKEN, /TOKENREFRESH, registration)
│   │   ├── SampleController.cs      # Weather feeds, email dispatch, multipart file upload
│   │   └── SongController.cs       # Paged API access to Billboard songs database
│   └── Mvc/                         # Server-Rendered HTML View Controllers (Cookie Auth)
│       ├── AccountController.cs     # Razor user login, register, profile management
│       ├── CrudsampleController.cs  # Product CRUD management forms and listings
│       ├── HomeController.cs        # Static homepage routing
│       └── WeatherForecastController.cs
├── Data/
│   └── ApplicationDbContext.cs      # EF Core database context & table mappings
├── Helpers/
│   └── ImageUploadExtension.cs      # GDI+ image scaling & thumbnail generator
├── Models/
│   ├── Dtos/                        # Data Transfer Objects for API requests/responses
│   ├── Entities/                    # EF Core Data Entities (Product, Song, UserAccount, RefreshToken)
│   └── ViewModels/                  # Razor View models
├── Services/
│   ├── EmailService.cs              # SMTP mailer service
│   └── TokenManager.cs              # JWT generation, validation & token revocation service
├── Views/                           # Razor HTML View templates
├── wwwroot/                         # Public static files, site CSS, glassmorphic homepage
├── Program.cs                       # Application entry point, DB check/seed, auth pipeline
├── run-debug.bat                    # x64 architecture launcher script
└── appsettings.json                 # DB connection string & JWT key parameters
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
Run via the helper script:
```powershell
.\Dotnet10MvcApi\run-debug.bat
```
Or directly via `dotnet run`:
```powershell
dotnet run --project Dotnet10MvcApi --arch x64
```

### Application Endpoints
*   **Web Portal Homepage:** [http://localhost:5071](http://localhost:5071)
*   **Scalar Interactive API Console:** [http://localhost:5071/scalar/v1](http://localhost:5071/scalar/v1) (or [http://localhost:5071/swagger](http://localhost:5071/swagger))
*   **OpenAPI Document Spec:** [http://localhost:5071/openapi/v1.json](http://localhost:5071/openapi/v1.json)
