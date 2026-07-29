using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dotnet10MvcApi.Services.Blazor
{
    /// <summary>
    /// Routing policy that disambiguates Blazor Razor component endpoints from MVC Controller endpoints.
    /// - Requests under /blazor (where PathBase is set by Path-Rewriter) ONLY match Blazor component endpoints.
    /// - Standard requests (where PathBase is empty) ONLY match MVC Controller / Web API endpoints.
    /// This eliminates AmbiguousMatchException between MVC /Account/Login and Blazor @page "/login".
    /// </summary>
    public class BlazorPathBaseEndpointSelectorPolicy : MatcherPolicy, IEndpointSelectorPolicy
    {
        public override int Order => -100;

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            return true;
        }

        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
        {
            var hasBlazorPathBase = !string.IsNullOrEmpty(httpContext.Request.PathBase.Value);

            for (int i = 0; i < candidates.Count; i++)
            {
                if (!candidates.IsValidCandidate(i)) continue;

                var endpoint = candidates[i].Endpoint;
                var isBlazor = IsBlazorEndpoint(endpoint);

                if (hasBlazorPathBase)
                {
                    // /blazor sub-path: ONLY Blazor endpoints are valid
                    candidates.SetValidity(i, isBlazor);
                }
                else
                {
                    // Non-blazor root path: ONLY Non-Blazor endpoints (MVC / API) are valid
                    candidates.SetValidity(i, !isBlazor);
                }
            }

            return Task.CompletedTask;
        }

        private static bool IsBlazorEndpoint(Endpoint endpoint)
        {
            if (endpoint == null) return false;

            // 1. MVC Controller Actions have ControllerActionDescriptor metadata
            if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() != null)
            {
                return false;
            }

            // 2. Blazor Razor Components have ComponentTypeMetadata metadata
            if (endpoint.Metadata.GetMetadata<ComponentTypeMetadata>() != null)
            {
                return true;
            }

            // 3. SignalR Hub metadata for Blazor Server ComponentHub
            var hubMetadata = endpoint.Metadata.GetMetadata<HubMetadata>();
            if (hubMetadata != null)
            {
                var hubTypeName = hubMetadata.HubType?.Name ?? string.Empty;
                var hubFullName = hubMetadata.HubType?.FullName ?? string.Empty;
                if (hubTypeName.Contains("ComponentHub") || hubFullName.Contains("Microsoft.AspNetCore.Components"))
                {
                    return true;
                }
            }

            // 4. Route pattern check for _blazor SignalR connection and negotiate endpoints
            if (endpoint is RouteEndpoint routeEndpoint)
            {
                var routePattern = routeEndpoint.RoutePattern?.RawText ?? string.Empty;
                if (routePattern.StartsWith("_blazor") || routePattern.Contains("/_blazor"))
                {
                    return true;
                }
            }

            // 5. Fallback string checks for Blazor root / fallback / hub endpoints
            var dn = endpoint.DisplayName ?? string.Empty;
            if (dn.Contains("Dotnet10MvcApi.Blazor") ||
                dn.Contains("RazorComponent") ||
                dn.Contains("Blazor") ||
                dn.Contains("ComponentHub") ||
                dn.Contains("_blazor"))
            {
                return true;
            }

            return false;
        }
    }
}

