using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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

        public AccessManager(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _userManager = userManager; // Initialize UserManager
        }

        public async Task<string> GenerateToken(ApplicationUser user)
        {
            // Expira em 1 hora, sempre em UTC (JWT usa timestamps UTC; usar hora local geraria
            // desalinhamento de expiração entre máquinas com timezones diferentes).
            var expires = DateTime.UtcNow.AddHours(1);

            // Get the user's roles
            var userRoles = await _userManager.GetRolesAsync(user);

            // Create the claims, including the roles.
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("id", user.Id.ToString()),
                new Claim("name", user.UserName ?? string.Empty),
                new Claim("profilePicture", user.ProfilePicture ?? "")
            };

            // Add each role as a claim.
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Resolve a chave da mesma forma que o Program.cs valida o token:
            // a variável de ambiente JWT_KEY tem prioridade, depois a seção Jwt:Key.
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
            if (string.IsNullOrWhiteSpace(jwtKey))
                jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("Chave JWT não configurada (defina a variável de ambiente JWT_KEY ou a seção Jwt:Key).");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
