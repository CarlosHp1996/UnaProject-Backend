using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnaProject.Application.Commands.Security;
using UnaProject.Application.Commands.Security.Handlers;
using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Application.Services;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Helpers;
using Xunit;

namespace UnaProject.Tests.Security
{
    /// <summary>
    /// Cobre a correção C-1 (bypass de auth no login social):
    ///  - o handler agora valida o accessToken via ISocialTokenValidator e SÓ processa a conta
    ///    com os dados VALIDADOS (nunca com dados enviados pelo cliente);
    ///  - se a validação falha, nenhuma conta é processada e o resultado é erro;
    ///  - o SocialTokenValidator rejeita provider desconhecido e token vazio sem fazer rede.
    /// </summary>
    public class SocialAuthTests
    {
        // ---------- ProcessSocialAuthCommandHandler ----------

        [Fact]
        public async Task Handler_Rejects_WhenTokenValidationFails()
        {
            var validator = new Mock<ISocialTokenValidator>();
            var failed = new ResultValue<SocialUserInfo>();
            failed.WithError("Invalid Google access token.");
            validator.Setup(v => v.ValidateAsync("google", "bad-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(failed);

            var service = new Mock<ISocialAuthService>(MockBehavior.Strict); // não pode ser chamado

            var handler = new ProcessSocialAuthCommandHandler(
                service.Object, validator.Object, NullLogger<ProcessSocialAuthCommandHandler>.Instance);

            var result = await handler.Handle(new ProcessSocialAuthCommand("google", "bad-token"), CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid Google access token.");
            service.Verify(s => s.ProcessSocialUserAsync(It.IsAny<SocialUserInfo>()), Times.Never);
        }

        [Fact]
        public async Task Handler_Rejects_WhenAccessTokenMissing()
        {
            var validator = new Mock<ISocialTokenValidator>(MockBehavior.Strict); // não pode ser chamado
            var service = new Mock<ISocialAuthService>(MockBehavior.Strict);

            var handler = new ProcessSocialAuthCommandHandler(
                service.Object, validator.Object, NullLogger<ProcessSocialAuthCommandHandler>.Instance);

            var result = await handler.Handle(new ProcessSocialAuthCommand("google", ""), CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Provider and access token are required.");
        }

        [Fact]
        public async Task Handler_UsesValidatedData_NotClientData()
        {
            var validated = new SocialUserInfo
            {
                Provider = "google",
                ProviderId = "google-sub-123",
                Email = "real@gmail.com",
                Name = "Real User"
            };

            var validator = new Mock<ISocialTokenValidator>();
            validator.Setup(v => v.ValidateAsync("google", "good-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResultValue<SocialUserInfo>.Success(validated));

            SocialUserInfo? processed = null;
            var service = new Mock<ISocialAuthService>();
            service.Setup(s => s.ProcessSocialUserAsync(It.IsAny<SocialUserInfo>()))
                .Callback<SocialUserInfo>(u => processed = u)
                .ReturnsAsync(new SocialAuthResponse { JwtToken = "jwt", Message = "ok" });

            var handler = new ProcessSocialAuthCommandHandler(
                service.Object, validator.Object, NullLogger<ProcessSocialAuthCommandHandler>.Instance);

            var result = await handler.Handle(new ProcessSocialAuthCommand("google", "good-token"), CancellationToken.None);

            result.HasSuccess.Should().BeTrue();
            processed.Should().NotBeNull();
            processed!.Email.Should().Be("real@gmail.com");
            processed.ProviderId.Should().Be("google-sub-123");
        }

        // ---------- SocialTokenValidator (sem rede) ----------

        private static SocialTokenValidator BuildValidator()
        {
            var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict); // não pode criar client nesses casos
            var config = new ConfigurationBuilder().Build();
            return new SocialTokenValidator(httpFactory.Object, config, NullLogger<SocialTokenValidator>.Instance);
        }

        [Fact]
        public async Task Validator_Rejects_UnsupportedProvider()
        {
            var validator = BuildValidator();

            var result = await validator.ValidateAsync("twitter", "some-token", CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Unsupported social provider.");
        }

        [Fact]
        public async Task Validator_Rejects_EmptyToken()
        {
            var validator = BuildValidator();

            var result = await validator.ValidateAsync("google", "", CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Access token is required.");
        }

        [Fact]
        public async Task Validator_Rejects_Facebook_WhenAppNotConfigured()
        {
            // Sem Authentication:Facebook:AppId/AppSecret => não valida (falha controlada, sem rede).
            var validator = BuildValidator();

            var result = await validator.ValidateAsync("facebook", "some-token", CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Facebook login is not configured.");
        }
    }
}
