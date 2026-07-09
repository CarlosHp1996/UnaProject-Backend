using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using UnaProject.Domain.Entities.Security;
using UnaProject.Domain.Security;
using Xunit;

namespace UnaProject.Tests.Security
{
    /// <summary>
    /// Testa a geração de JWT em AccessManager (Domain/Security/AccessManager.cs):
    ///  - token válido e parseável, assinado com HS256;
    ///  - claims esperados (sub, id, name, roles);
    ///  - resolução da chave (Jwt:Key) e falha quando não há chave configurada.
    ///
    /// Nota: GenerateToken lê a variável de ambiente JWT_KEY com prioridade sobre Jwt:Key.
    /// Os testes salvam/limpam/restauram JWT_KEY para serem determinísticos.
    /// </summary>
    public class AccessManagerTests
    {
        private const string TestKey = "chave-super-secreta-de-teste-com-mais-de-32-bytes-1234567890";

        private static Mock<UserManager<ApplicationUser>> BuildUserManagerMock(IList<string> roles)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mgr = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            mgr.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
               .ReturnsAsync(roles);

            return mgr;
        }

        private static Mock<IConfiguration> BuildConfig(string? jwtKey)
        {
            var config = new Mock<IConfiguration>();
            config.SetupGet(c => c["Jwt:Key"]).Returns(jwtKey);
            config.SetupGet(c => c["Jwt:Issuer"]).Returns("una-issuer");
            config.SetupGet(c => c["Jwt:Audience"]).Returns("una-audience");
            return config;
        }

        private static ApplicationUser BuildUser() => new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "caique",
            ProfilePicture = "https://img/foo.png"
        };

        [Fact]
        public async Task GenerateToken_ProducesParseableSignedJwt_WithExpectedClaims()
        {
            var original = Environment.GetEnvironmentVariable("JWT_KEY");
            Environment.SetEnvironmentVariable("JWT_KEY", null);
            try
            {
                var user = BuildUser();
                var userManager = BuildUserManagerMock(new List<string> { "Admin", "User" });
                var config = BuildConfig(TestKey);
                var sut = new AccessManager(config.Object, userManager.Object);

                var token = await sut.GenerateToken(user);

                token.Should().NotBeNullOrWhiteSpace();

                var handler = new JwtSecurityTokenHandler();
                handler.CanReadToken(token).Should().BeTrue();

                var jwt = handler.ReadJwtToken(token);

                // Assinado com HS256.
                jwt.Header.Alg.Should().Be("HS256");
                // Três segmentos (header.payload.signature) => está assinado.
                token.Split('.').Should().HaveCount(3);
                token.Split('.')[2].Should().NotBeNullOrEmpty();

                jwt.Issuer.Should().Be("una-issuer");
                jwt.Audiences.Should().Contain("una-audience");

                // Claim "id" e "name".
                jwt.Claims.First(c => c.Type == "id").Value.Should().Be(user.Id.ToString());
                jwt.Claims.First(c => c.Type == "name").Value.Should().Be("caique");

                // "sub" (JwtRegisteredClaimNames.Sub) = user.Id.
                jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value
                   .Should().Be(user.Id.ToString());

                // "jti" presente e é um GUID.
                var jti = jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
                Guid.TryParse(jti, out _).Should().BeTrue();

                // Roles (ClaimTypes.Role) contêm Admin e User.
                var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
                roleClaims.Should().Contain(new[] { "Admin", "User" });
            }
            finally
            {
                Environment.SetEnvironmentVariable("JWT_KEY", original);
            }
        }

        [Fact]
        public async Task GenerateToken_WorksWithNoRoles()
        {
            var original = Environment.GetEnvironmentVariable("JWT_KEY");
            Environment.SetEnvironmentVariable("JWT_KEY", null);
            try
            {
                var user = BuildUser();
                var userManager = BuildUserManagerMock(new List<string>());
                var config = BuildConfig(TestKey);
                var sut = new AccessManager(config.Object, userManager.Object);

                var token = await sut.GenerateToken(user);

                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
                jwt.Claims.Any(c => c.Type == ClaimTypes.Role).Should().BeFalse();
                jwt.Claims.First(c => c.Type == "id").Value.Should().Be(user.Id.ToString());
            }
            finally
            {
                Environment.SetEnvironmentVariable("JWT_KEY", original);
            }
        }

        [Fact]
        public async Task GenerateToken_UsesEnvJwtKey_WhenPresent()
        {
            var original = Environment.GetEnvironmentVariable("JWT_KEY");
            Environment.SetEnvironmentVariable("JWT_KEY", TestKey);
            try
            {
                var user = BuildUser();
                var userManager = BuildUserManagerMock(new List<string>());
                // Config SEM Jwt:Key => precisa cair no env var.
                var config = BuildConfig(jwtKey: null);
                var sut = new AccessManager(config.Object, userManager.Object);

                var token = await sut.GenerateToken(user);

                token.Should().NotBeNullOrWhiteSpace();
                new JwtSecurityTokenHandler().CanReadToken(token).Should().BeTrue();
            }
            finally
            {
                Environment.SetEnvironmentVariable("JWT_KEY", original);
            }
        }

        [Fact]
        public async Task GenerateToken_Throws_WhenNoKeyConfigured()
        {
            var original = Environment.GetEnvironmentVariable("JWT_KEY");
            Environment.SetEnvironmentVariable("JWT_KEY", null);
            try
            {
                var user = BuildUser();
                var userManager = BuildUserManagerMock(new List<string>());
                var config = BuildConfig(jwtKey: null); // nem env nem Jwt:Key.
                var sut = new AccessManager(config.Object, userManager.Object);

                var act = async () => await sut.GenerateToken(user);

                await act.Should().ThrowAsync<InvalidOperationException>()
                         .WithMessage("*Chave JWT*");
            }
            finally
            {
                Environment.SetEnvironmentVariable("JWT_KEY", original);
            }
        }
    }
}
