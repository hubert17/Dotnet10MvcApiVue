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
        /// Formatted base href string for HTML head, e.g. "/blazor/" or "/app2/".
        /// </summary>
        public string BaseHref => $"/{RoutePrefix.Trim('/')}/";
    }
}
