using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Services;
using Xunit;

namespace UnaProject.Tests.Payments
{
    /// <summary>
    /// Testa a resolução de gateway (Application/Services/PaymentGatewayResolver.cs).
    /// Confirma o roteamento por método ("card"/"pix") e por provedor ("stripe"),
    /// a insensibilidade a maiúsculas/minúsculas e o comportamento REAL de erro
    /// (lança InvalidOperationException para método/provedor desconhecido ou vazio).
    /// </summary>
    public class PaymentGatewayResolverTests
    {
        private static IPaymentGateway MakeStripeGateway()
        {
            var gw = new Mock<IPaymentGateway>();
            gw.SetupGet(g => g.ProviderName).Returns("stripe");
            gw.SetupGet(g => g.SupportedMethods).Returns(new[] { "card", "pix" });
            return gw.Object;
        }

        private static PaymentGatewayResolver BuildResolver(IPaymentGateway stripe)
        {
            var byMethod = new Dictionary<string, IPaymentGateway>
            {
                ["card"] = stripe,
                ["pix"] = stripe
            };
            var byProvider = new Dictionary<string, IPaymentGateway>
            {
                ["stripe"] = stripe
            };
            return new PaymentGatewayResolver(byMethod, byProvider);
        }

        [Theory]
        [InlineData("card")]
        [InlineData("pix")]
        public void Resolve_ReturnsGateway_ForSupportedMethod(string method)
        {
            var stripe = MakeStripeGateway();
            var resolver = BuildResolver(stripe);

            var gateway = resolver.Resolve(method);

            gateway.Should().BeSameAs(stripe);
        }

        [Theory]
        [InlineData("CARD")]
        [InlineData("Pix")]
        public void Resolve_IsCaseInsensitive(string method)
        {
            var stripe = MakeStripeGateway();
            var resolver = BuildResolver(stripe);

            var gateway = resolver.Resolve(method);

            gateway.Should().BeSameAs(stripe);
        }

        [Fact]
        public void ResolveByProvider_ReturnsGateway_ForStripe()
        {
            var stripe = MakeStripeGateway();
            var resolver = BuildResolver(stripe);

            var gateway = resolver.ResolveByProvider("stripe");

            gateway.Should().BeSameAs(stripe);
        }

        [Fact]
        public void ResolveByProvider_IsCaseInsensitive()
        {
            var stripe = MakeStripeGateway();
            var resolver = BuildResolver(stripe);

            var gateway = resolver.ResolveByProvider("STRIPE");

            gateway.Should().BeSameAs(stripe);
        }

        [Theory]
        [InlineData("boleto")]
        [InlineData("unknown")]
        [InlineData("")]
        [InlineData("   ")]
        public void Resolve_Throws_ForUnknownOrEmptyMethod(string method)
        {
            var resolver = BuildResolver(MakeStripeGateway());

            var act = () => resolver.Resolve(method);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*gateway de pagamento*");
        }

        [Fact]
        public void Resolve_Throws_ForNullMethod()
        {
            var resolver = BuildResolver(MakeStripeGateway());

            var act = () => resolver.Resolve(null!);

            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData("paypal")]
        [InlineData("")]
        [InlineData("   ")]
        public void ResolveByProvider_Throws_ForUnknownOrEmptyProvider(string provider)
        {
            var resolver = BuildResolver(MakeStripeGateway());

            var act = () => resolver.ResolveByProvider(provider);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*gateway de pagamento*");
        }

        [Fact]
        public void ResolveByProvider_Throws_ForNullProvider()
        {
            var resolver = BuildResolver(MakeStripeGateway());

            var act = () => resolver.ResolveByProvider(null!);

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
