namespace Dotnet10MvcApi.Blazor
{
    public class BlazorOptions
    {
        /// <summary>
        /// Route prefix for Blazor Server (e.g. "blazor", "app2", "portal").
        /// Configured in appsettings.json under "BlazorSettings:RoutePrefix".
        /// </summary>
        public string RoutePrefix { get; set; } = "blazor";

        /// <summary>
        /// App name displayed in Blazor layout headers.
        /// Configured in appsettings.json under "BlazorSettings:AppName".
        /// </summary>
        public string AppName { get; set; } = "Dotnet10 Blazor";

        /// <summary>
        /// Formatted base href string for HTML head, e.g. "/blazor/" or "/app2/".
        /// </summary>
        public string BaseHref => $"/{RoutePrefix.Trim('/')}/";

        /// <summary>
        /// Formatted home path string for Blazor home (e.g. "/blazor").
        /// </summary>
        public string HomePath => string.IsNullOrWhiteSpace(RoutePrefix) ? "/" : $"/{RoutePrefix.Trim('/')}";
    }
}
