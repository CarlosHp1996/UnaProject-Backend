using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UnaProject.Application.Services;
using Xunit;

namespace UnaProject.Tests.Payments
{
    /// <summary>
    /// Testa a lógica pura de StripeGateway (Application/Services/StripeGateway.cs)
    /// SEM chamadas de rede ao Stripe:
    ///  - ValidateWebhookSignatureAsync: assinatura/secret ausentes ou inválidos.
    ///  - MapStatus (privado) testado INDIRETAMENTE via ParseWebhookEventAsync,
    ///    que apenas desserializa o payload (EventUtility.ParseEvent) e não faz rede.
    /// </summary>
    public class StripeGatewayTests
    {
        private static StripeGateway BuildGateway(string? webhookSecret = null)
        {
            var options = Options.Create(new StripeOptions
            {
                STRIPE_SECRET_KEY = "sk_test_dummy",
                STRIPE_WEBHOOK_SECRET = webhookSecret
            });

            return new StripeGateway(options, NullLogger<StripeGateway>.Instance);
        }

        // ---------- ValidateWebhookSignatureAsync ----------

        [Fact]
        public async Task ValidateWebhook_ReturnsError_WhenSignatureMissing()
        {
            var gateway = BuildGateway(webhookSecret: "whsec_test");

            var result = await gateway.ValidateWebhookSignatureAsync("{}", signature: null, secret: null);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Webhook signature or secret not provided");
        }

        [Fact]
        public async Task ValidateWebhook_ReturnsError_WhenSecretMissing()
        {
            // Sem secret no options e sem secret no parâmetro => webhookSecret nulo.
            var gateway = BuildGateway(webhookSecret: null);

            var result = await gateway.ValidateWebhookSignatureAsync("{}", signature: "t=1,v1=abc", secret: null);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Webhook signature or secret not provided");
        }

        [Fact]
        public async Task ValidateWebhook_ReturnsInvalid_WhenSignatureBogus()
        {
            var gateway = BuildGateway(webhookSecret: "whsec_test");

            // Assinatura malformada => EventUtility.ConstructEvent lança StripeException,
            // que o código traduz para "Invalid webhook signature".
            var result = await gateway.ValidateWebhookSignatureAsync(
                rawPayload: "{\"id\":\"evt_1\"}",
                signature: "bogus-signature",
                secret: "whsec_test");

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid webhook signature");
        }

        [Fact]
        public async Task ValidateWebhook_ReturnsInvalid_WhenSignatureWrongForSecret()
        {
            var gateway = BuildGateway();

            // Formato de header correto (t=,v1=) mas HMAC não confere com o secret.
            var result = await gateway.ValidateWebhookSignatureAsync(
                rawPayload: "{\"id\":\"evt_1\"}",
                signature: "t=1700000000,v1=0000000000000000000000000000000000000000000000000000000000000000",
                secret: "whsec_test");

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid webhook signature");
        }

        // ---------- MapStatus (indireto via ParseWebhookEventAsync) ----------

        private static string BuildEventJson(string paymentIntentStatus) =>
            "{" +
            "\"id\":\"evt_test\"," +
            "\"object\":\"event\"," +
            "\"api_version\":\"2022-11-15\"," +
            "\"type\":\"payment_intent.updated\"," +
            "\"data\":{\"object\":{" +
                "\"id\":\"pi_test\"," +
                "\"object\":\"payment_intent\"," +
                $"\"status\":\"{paymentIntentStatus}\"" +
            "}}}";

        [Theory]
        [InlineData("succeeded", "succeeded")]
        [InlineData("canceled", "cancelled")]      // "canceled" (Stripe) -> "cancelled" (domínio)
        [InlineData("processing", "processing")]
        [InlineData("requires_payment_method", "pending")]
        [InlineData("requires_confirmation", "pending")]
        [InlineData("requires_action", "pending")]
        [InlineData("requires_capture", "pending")]
        [InlineData("some_future_status", "some_future_status")] // passthrough do default
        public async Task ParseWebhookEvent_MapsStripeStatusToDomainStatus(string stripeStatus, string expectedDomainStatus)
        {
            var gateway = BuildGateway();

            var result = await gateway.ParseWebhookEventAsync(BuildEventJson(stripeStatus));

            result.HasSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.Status.Should().Be(expectedDomainStatus);
            result.Value.ExternalId.Should().Be("pi_test");
            result.Value.EventType.Should().Be("payment_intent.updated");
        }

        [Fact]
        public async Task ParseWebhookEvent_ReturnsError_WhenPayloadInvalid()
        {
            var gateway = BuildGateway();

            var result = await gateway.ParseWebhookEventAsync("not-json-at-all");

            result.HasSuccess.Should().BeFalse();
        }
    }
}
