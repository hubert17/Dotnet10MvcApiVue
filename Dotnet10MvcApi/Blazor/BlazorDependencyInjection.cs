using Blazored.LocalStorage;
using BlazorState;
using Dotnet10MvcApi.Services.Blazor;
using Dotnet10MvcApi.Services.Notifications;
using Dotnet10MvcApi.Blazor.States;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudExtensions.Services;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dotnet10MvcApi.Blazor
{
    public static class BlazorDependencyInjection
    {
        public static IServiceCollection AddBlazorCore(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            services.Configure<BlazorOptions>(configuration.GetSection("BlazorSettings"));
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<BlazorOptions>>().Value);
            services.AddBlazoredLocalStorage();
            services.AddBlazorState(opts =>
                opts.Assemblies = new[] { typeof(CounterState).GetTypeInfo().Assembly });

            services.AddHttpContextAccessor();
            services.AddScoped<Dotnet10MvcApi.Services.UserAccountService>();

            // Auth — shared cookie with MVC, no interface abstraction
            services.AddAuthorizationCore();
            services.AddScoped<BlazorUserService>();
            services.AddScoped<ServerCookieAuthService>();
            services.AddScoped<HostedAuthStateProvider>();
            services.AddScoped<AuthenticationStateProvider>(
                sp => sp.GetRequiredService<HostedAuthStateProvider>());

            // Notification — concrete class, no interface
            services.AddScoped<NotificationService>();

            // MudBlazor UI
            services.AddMudServices();
            services.AddMudExtensions();

            return services;
        }
    }
}
