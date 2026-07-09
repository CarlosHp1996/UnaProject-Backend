using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos.Gateway;
using UnaProject.Application.Models.Requests.Payments;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Enums;
using UnaProject.Domain.Helpers;
using UnaProject.Infra.Data;
using UnaProject.Infra.Repositories;
using Xunit;

namespace UnaProject.Tests.Payments
{
    /// <summary>
    /// Testes de segurança do PaymentRepository (C-2 valor/IDOR e A-2 conferência de webhook)
    /// usando EF Core InMemory. Cobrem:
    ///  - criação rejeita valor divergente do total do pedido;
    ///  - criação usa order.TotalAmount (não o valor do cliente);
    ///  - IDOR: não-dono não cria/cancela/consulta pagamento alheio;
    ///  - webhook com valor divergente NÃO marca pago (segura para revisão).
    /// </summary>
    public class PaymentRepositorySecurityTests
    {
        private static AppDbContext BuildContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            return new AppDbContext(options);
        }

        private static PaymentRepository BuildRepository(AppDbContext context, IPaymentGatewayResolver? resolver = null)
        {
            resolver ??= new Mock<IPaymentGatewayResolver>(MockBehavior.Strict).Object;
            return new PaymentRepository(context, resolver, NullLogger<PaymentRepository>.Instance);
        }

        private static Order SeedOrder(AppDbContext context, Guid ownerId, decimal total)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = ownerId,
                TotalAmount = total,
                PaymentStatus = EnumPaymentStatus.Pending,
                Status = EnumOrderStatus.Pending
            };
            context.Orders.Add(order);
            context.SaveChanges();
            return order;
        }

        // ---------- C-2: valor do pagamento ----------

        [Fact]
        public async Task CreatePayment_Rejects_WhenClientAmountDivergesFromOrderTotal()
        {
            using var context = BuildContext();
            var userId = Guid.NewGuid();
            var order = SeedOrder(context, userId, total: 150.00m);

            // Resolver estrito: se for chamado, o teste falha (a rejeição deve ocorrer ANTES do gateway).
            var repo = BuildRepository(context, new Mock<IPaymentGatewayResolver>(MockBehavior.Strict).Object);

            var request = new CreateAbacatePaymentRequest
            {
                OrderId = order.Id,
                Amount = 1.00m, // divergente do total (150)
                PaymentMethod = "card",
                CustomerName = "Attacker",
                UserId = userId,
                IsAdmin = false
            };

            var result = await repo.CreateAbacatePayment(request, CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Payment amount does not match the order total.");
        }

        [Fact]
        public async Task CreatePayment_UsesOrderTotal_NotClientAmount()
        {
            using var context = BuildContext();
            var userId = Guid.NewGuid();
            var order = SeedOrder(context, userId, total: 150.00m);

            GatewayPaymentRequest? captured = null;
            var gateway = new Mock<IPaymentGateway>();
            gateway.SetupGet(g => g.ProviderName).Returns("stripe");
            gateway.Setup(g => g.CreatePaymentAsync(It.IsAny<GatewayPaymentRequest>(), It.IsAny<CancellationToken>()))
                .Callback<GatewayPaymentRequest, CancellationToken>((req, _) => captured = req)
                .ReturnsAsync(Result<GatewayPaymentResult>.Success(new GatewayPaymentResult
                {
                    ExternalId = "pi_test",
                    ClientSecret = "pi_test_secret",
                    Status = "pending"
                }));

            var resolver = new Mock<IPaymentGatewayResolver>();
            resolver.Setup(r => r.Resolve(It.IsAny<string>())).Returns(gateway.Object);

            var repo = BuildRepository(context, resolver.Object);

            var request = new CreateAbacatePaymentRequest
            {
                OrderId = order.Id,
                Amount = 0m, // cliente não informa valor
                PaymentMethod = "card",
                CustomerName = "Legit Buyer",
                UserId = userId,
                IsAdmin = false
            };

            var result = await repo.CreateAbacatePayment(request, CancellationToken.None);

            result.HasSuccess.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.Amount.Should().Be(150.00m);
            result.Value!.Amount.Should().Be(150.00m);
        }

        // ---------- C-2: IDOR ----------

        [Fact]
        public async Task CreatePayment_NonOwner_ReturnsNotFound()
        {
            using var context = BuildContext();
            var owner = Guid.NewGuid();
            var attacker = Guid.NewGuid();
            var order = SeedOrder(context, owner, total: 100.00m);

            var repo = BuildRepository(context, new Mock<IPaymentGatewayResolver>(MockBehavior.Strict).Object);

            var request = new CreateAbacatePaymentRequest
            {
                OrderId = order.Id,
                Amount = 100.00m,
                PaymentMethod = "card",
                CustomerName = "Attacker",
                UserId = attacker,
                IsAdmin = false
            };

            var result = await repo.CreateAbacatePayment(request, CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Order not found");
        }

        [Fact]
        public async Task CancelPayment_NonOwner_ReturnsNotFound()
        {
            using var context = BuildContext();
            var owner = Guid.NewGuid();
            var attacker = Guid.NewGuid();
            var order = SeedOrder(context, owner, total: 100.00m);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = 100.00m,
                Status = "pending",
                PaymentMethod = "card",
                CreatedAt = DateTime.UtcNow
            };
            context.Payments.Add(payment);
            context.SaveChanges();

            var repo = BuildRepository(context, new Mock<IPaymentGatewayResolver>(MockBehavior.Strict).Object);

            var result = await repo.CancelPayment(payment.Id, "trying", attacker, isAdmin: false, CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Payment not found");

            // Não deve ter cancelado nada.
            var stored = await context.Payments.FindAsync(payment.Id);
            stored!.Status.Should().Be("pending");
        }

        [Fact]
        public async Task GetPaymentStatus_NonOwner_ReturnsNotFound()
        {
            using var context = BuildContext();
            var owner = Guid.NewGuid();
            var attacker = Guid.NewGuid();
            var order = SeedOrder(context, owner, total: 100.00m);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = 100.00m,
                Status = "pending",
                PaymentMethod = "card",
                CreatedAt = DateTime.UtcNow
            };
            context.Payments.Add(payment);
            context.SaveChanges();

            var repo = BuildRepository(context, new Mock<IPaymentGatewayResolver>(MockBehavior.Strict).Object);

            var result = await repo.GetPaymentStatus(payment.Id, attacker, isAdmin: false, CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Payment not found");
        }

        [Fact]
        public async Task GetPaymentStatus_Admin_CanReadAnyPayment()
        {
            using var context = BuildContext();
            var owner = Guid.NewGuid();
            var order = SeedOrder(context, owner, total: 100.00m);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = 100.00m,
                Status = "succeeded", // não-pending: não tenta sincronizar com gateway
                PaymentMethod = "card",
                CreatedAt = DateTime.UtcNow
            };
            context.Payments.Add(payment);
            context.SaveChanges();

            var repo = BuildRepository(context, new Mock<IPaymentGatewayResolver>(MockBehavior.Strict).Object);

            var result = await repo.GetPaymentStatus(payment.Id, Guid.NewGuid(), isAdmin: true, CancellationToken.None);

            result.HasSuccess.Should().BeTrue();
            result.Value!.PaymentId.Should().Be(payment.Id);
        }

        // ---------- A-2: conferência de valor no webhook ----------

        [Fact]
        public async Task ProcessWebhook_AmountMismatch_DoesNotMarkPaid()
        {
            using var context = BuildContext();
            var order = SeedOrder(context, Guid.NewGuid(), total: 100.00m);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = 100.00m, // esperado = 10000 centavos
                Currency = "BRL",
                Status = "pending",
                PaymentMethod = "card",
                TransactionId = "pi_mismatch",
                BillingId = "pi_mismatch",
                CreatedAt = DateTime.UtcNow
            };
            context.Payments.Add(payment);
            context.SaveChanges();

            var repo = BuildRepository(context);

            // Gateway reporta apenas 50.00 (5000 centavos) — divergente.
            var result = await repo.ProcessPaymentWebhook(
                billingId: "pi_mismatch",
                status: "succeeded",
                fee: null,
                paidAt: DateTime.UtcNow,
                metadata: "{}",
                amountReceivedCents: 5000,
                currency: "brl",
                CancellationToken.None);

            result.HasSuccess.Should().BeFalse();

            var storedOrder = await context.Orders.FindAsync(order.Id);
            storedOrder!.PaymentStatus.Should().Be(EnumPaymentStatus.Pending); // NÃO foi liberado

            var storedPayment = await context.Payments.FindAsync(payment.Id);
            storedPayment!.Status.Should().Be("review");
        }

        [Fact]
        public async Task ProcessWebhook_AmountMatches_MarksPaid()
        {
            using var context = BuildContext();
            var order = SeedOrder(context, Guid.NewGuid(), total: 100.00m);

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = 100.00m, // esperado = 10000 centavos
                Currency = "BRL",
                Status = "pending",
                PaymentMethod = "card",
                TransactionId = "pi_ok",
                BillingId = "pi_ok",
                CreatedAt = DateTime.UtcNow
            };
            context.Payments.Add(payment);
            context.SaveChanges();

            var repo = BuildRepository(context);

            var result = await repo.ProcessPaymentWebhook(
                billingId: "pi_ok",
                status: "succeeded",
                fee: null,
                paidAt: DateTime.UtcNow,
                metadata: "{}",
                amountReceivedCents: 10000,
                currency: "brl",
                CancellationToken.None);

            result.HasSuccess.Should().BeTrue();

            var storedOrder = await context.Orders.FindAsync(order.Id);
            storedOrder!.PaymentStatus.Should().Be(EnumPaymentStatus.Paid);

            var storedPayment = await context.Payments.FindAsync(payment.Id);
            storedPayment!.Status.Should().Be("succeeded");
        }
    }
}
