namespace Dotnet10MvcApi.Models;

/// <summary>
/// Strongly-typed configuration options bound from "MvcSettings" in appsettings.json.
/// </summary>
public class MvcOptions
{
    public string? HomeRoute { get; set; }
    public string AppName { get; set; } = "My ASP.NET Webapp";
    public string AppDescription { get; set; } = "A modern multi-paradigm web application built with ASP.NET Core.";

    /// <summary>
    /// Formatted route path for the MVC portal home (e.g. "/portal" or "/").
    /// </summary>
    public string HomePath => string.IsNullOrWhiteSpace(HomeRoute) ? "/" : $"/{HomeRoute.Trim('/')}";
}
