using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace Dotnet10MvcApi.Services
{
    /// <summary>
    /// Custom IAntiforgery wrapper that bypasses anti-forgery validation for Piranha Manager API routes (/manager/api/*).
    /// </summary>
    public class BypassManagerAntiforgery : IAntiforgery
    {
        private readonly IAntiforgery _inner;

        public BypassManagerAntiforgery(IAntiforgery inner)
        {
            _inner = inner;
        }

        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
        {
            return _inner.GetAndStoreTokens(httpContext);
        }

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
        {
            return _inner.GetTokens(httpContext);
        }

        public Task<bool> IsRequestValidAsync(HttpContext httpContext)
        {
            if (httpContext.Request.Path.StartsWithSegments("/manager/api"))
            {
                return Task.FromResult(true);
            }
            return _inner.IsRequestValidAsync(httpContext);
        }

        public Task ValidateRequestAsync(HttpContext httpContext)
        {
            if (httpContext.Request.Path.StartsWithSegments("/manager/api"))
            {
                return Task.CompletedTask;
            }
            return _inner.ValidateRequestAsync(httpContext);
        }

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
            _inner.SetCookieTokenAndHeader(httpContext);
        }
    }
}
