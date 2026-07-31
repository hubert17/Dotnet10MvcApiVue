using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnet10MvcApi.Services.Cms
{
    /// <summary>
    /// Middleware for Piranha Manager UI: Populates Anti-Forgery XSRF-TOKEN cookies and injects contrast & logout fix CSS/JS.
    /// </summary>
    public class PiranhaManagerAssetsMiddleware
    {
        private readonly RequestDelegate _next;

        public PiranhaManagerAssetsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/manager"))
            {
                var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                var tokens = antiforgery.GetAndStoreTokens(context);
                if (!string.IsNullOrEmpty(tokens.RequestToken))
                {
                    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
                    {
                        HttpOnly = false,
                        SameSite = SameSiteMode.Lax,
                        Secure = context.Request.IsHttps
                    });
                }

                if (!context.Request.Path.StartsWithSegments("/manager/api") &&
                    !context.Request.Path.StartsWithSegments("/manager/assets"))
                {
                    var originalBodyStream = context.Response.Body;
                    using var memoryStream = new MemoryStream();
                    context.Response.Body = memoryStream;

                    await _next(context);

                    if (context.Response.ContentType != null && context.Response.ContentType.Contains("text/html"))
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        using var reader = new StreamReader(memoryStream, Encoding.UTF8, leaveOpen: true);
                        var html = await reader.ReadToEndAsync();

                        const string managerScriptFix = @"<style id='piranha-contrast-fix'>
.text-light,.text-white,[class*='text-light'],[class*='text-white']{color:#334155!important;}
.nav-item.nav-header a, a.navbar-text, [href*='logout'] { cursor: pointer !important; }
</style>
<script id='piranha-logout-fix'>
document.addEventListener('DOMContentLoaded', function() {
    var setupLogout = function() {
        document.querySelectorAll('.nav-item.nav-header a, a.navbar-text').forEach(function(el) {
            if (el.textContent && el.textContent.trim().toLowerCase().includes('logout')) {
                el.setAttribute('href', '/manager/logout?returnUrl=/manager');
                el.style.cursor = 'pointer';
            }
        });
    };
    setupLogout();
    setTimeout(setupLogout, 500);
    document.addEventListener('click', function(e) {
        var target = e.target.closest('a');
        if (target && target.textContent && target.textContent.trim().toLowerCase().includes('logout')) {
            e.preventDefault();
            window.location.href = '/manager/logout?returnUrl=/manager';
        }
    });
});
</script>";
                        if (html.Contains("</head>"))
                        {
                            html = html.Replace("</head>", $"{managerScriptFix}</head>");
                        }

                        var bytes = Encoding.UTF8.GetBytes(html);
                        context.Response.ContentLength = bytes.Length;
                        await originalBodyStream.WriteAsync(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        await memoryStream.CopyToAsync(originalBodyStream);
                    }
                    context.Response.Body = originalBodyStream;
                    return;
                }
            }
            await _next(context);
        }
    }

    public static class PiranhaManagerAssetsMiddlewareExtensions
    {
        public static IApplicationBuilder UsePiranhaManagerAssets(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PiranhaManagerAssetsMiddleware>();
        }
    }
}
