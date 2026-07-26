using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Dotnet10MvcApi.Models;

namespace Dotnet10MvcApi.Services
{
    public class DevUserService
    {
        private readonly List<DevUserConfig> _devUsers;

        public DevUserService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _devUsers = configuration.GetSection("DevUsers").Get<List<DevUserConfig>>() ?? new List<DevUserConfig>();

            // Fallback: If DevUsers section is empty (e.g. when app runs without ASPNETCORE_ENVIRONMENT=Development),
            // explicitly load appsettings.Development.json so dev accounts always work in local development.
            if (!_devUsers.Any())
            {
                var devSettingsPath = Path.Combine(env.ContentRootPath, "appsettings.Development.json");
                if (File.Exists(devSettingsPath))
                {
                    try
                    {
                        var devConfig = new ConfigurationBuilder()
                            .AddJsonFile(devSettingsPath, optional: true, reloadOnChange: false)
                            .Build();
                        _devUsers = devConfig.GetSection("DevUsers").Get<List<DevUserConfig>>() ?? new List<DevUserConfig>();
                    }
                    catch { }
                }
            }
        }

        public DevUserConfig? ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            var cleanUsername = username.Trim();
            var cleanPassword = password.Trim();
            
            return _devUsers.FirstOrDefault(u =>
                u.Username.Equals(cleanUsername, StringComparison.OrdinalIgnoreCase) &&
                (u.Password == cleanPassword || u.Password == password));
        }

        public DevUserConfig? GetUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var cleanUsername = username.Trim();
            return _devUsers.FirstOrDefault(u =>
                u.Username.Equals(cleanUsername, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsDevUser(string username)
        {
            return GetUser(username) != null;
        }
    }
}
