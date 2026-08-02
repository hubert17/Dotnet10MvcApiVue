using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace Dotnet10MvcApi.Services
{
    public class TokenManager
    {
        private readonly IConfiguration _configuration;
        private readonly IHostEnvironment _env;

        public TokenManager(IConfiguration configuration, IHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        public string CreateToken(string username, string[]? roles = null, int? expireMinutes = null)
        {
            var secret = GetJwtSecret();
            var issuer = _configuration["JwtSettings:Issuer"] ?? "Dotnet10MvcApi";
            var audience = _configuration["JwtSettings:Audience"] ?? "Dotnet10MvcApi";
            var expiry = expireMinutes ?? int.Parse(_configuration["JwtSettings:ExpiryMinutes"] ?? "20");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, username)
            };

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
                    }
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiry),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var secret = GetJwtSecret();
            
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, // Match old validator simplicity
                ValidateAudience = false,
                ValidateLifetime = false, // Read claims from expired token
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                return principal;
            }
            catch
            {
                return null;
            }
        }

        private string GetJwtSecret()
        {
            var secret = _configuration["JwtSettings:Secret"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                if (_env.IsDevelopment())
                    return "f848bcae3399961afba711f8ced6fc3c"; // Dev-only fallback
                throw new InvalidOperationException("JwtSettings:Secret is required in non-Development environments.");
            }
            return secret;
        }
    }
}
