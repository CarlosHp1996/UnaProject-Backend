using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UnaProject.Domain.Entities.Security;

namespace UnaProject.Domain.Security
{
    public class AccessManager
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private static readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new();

        public AccessManager(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager; // Initialize UserManager
        }

        public async Task<string> GenerateToken(ApplicationUser user)
        {
            var expiresLocal = DateTime.Now.AddHours(1); // Expires in 1 hour local time.
            Console.WriteLine($"Token expires at: {expiresLocal} (Local)");
            Console.WriteLine($"Token expires at: {expiresLocal.ToUniversalTime()} (UTC)");

            // Get the user's roles
            var userRoles = await _userManager.GetRolesAsync(user);

            // Create the claims, including the roles.
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("id", user.Id.ToString()),
                new Claim("name", user.UserName)
            };

            // Add each role as a claim.
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT_KEY"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresLocal,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task InvalidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                return; // Token inválido, nada a fazer
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);
            var expires = jwtToken.ValidTo;

            // Add the token to the blacklist until its expiration date.
            _blacklistedTokens.TryAdd(token, expires);
            await Task.CompletedTask;

            // Optional: Clearing expired tokens
            CleanupExpiredTokens();
        }

        public static bool IsTokenBlacklisted(string token)
        {
            if (_blacklistedTokens.TryGetValue(token, out DateTime expires))
            {
                if (DateTime.UtcNow <= expires)
                {
                    return true; // The token is on the blacklist and has not yet expired.
                }
                else
                {
                    // Remove expired token from blacklist.
                    _blacklistedTokens.TryRemove(token, out _);
                }
            }
            return false;
        }

        private void CleanupExpiredTokens()
        {
            var now = DateTime.UtcNow;
            var expiredTokens = _blacklistedTokens
                .Where(kvp => kvp.Value < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in expiredTokens)
            {
                _blacklistedTokens.TryRemove(token, out _);
            }
        }
    }
}
