using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UnaProject.Application.Commands.Payments;
using UnaProject.Application.Commands.Payments.Handlers;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Requests.Payments;
using UnaProject.Domain.Helpers;
using DtoResponse = UnaProject.Application.Models.Dtos.CreateAbacatePaymentResponse;
using Xunit;

namespace UnaProject.Tests.Payments
{
    /// <summary>
    /// Testa os handlers CQRS de pagamento com IPaymentRepository mockado
    /// (sem banco, sem gateway real):
    ///  - CreatePaymentCommandHandler
    ///  - CancelPaymentCommandHandler
    /// Cobre sucesso (propaga resultado do repo), falha do repo (propaga erro)
    /// e exceção do repo (tratada como erro interno).
    /// </summary>
    public class PaymentHandlersTests
    {
        private static CreatePaymentCommand BuildCreateCommand() => new CreatePaymentCommand(
            OrderId: Guid.NewGuid(),
            Amount: 199.90m,
            PaymentMethod: "card",
            CustomerName: "Caique",
            CustomerDocument: "12345678900",
            CustomerEmail: "caique@example.com",
            CustomerPhone: "+5511999999999",
            ReturnUrl: "https://una/return",
            Metadata: null);

        // ---------- CreatePaymentCommandHandler ----------

        [Fact]
        public async Task CreatePayment_ReturnsSuccess_WhenRepositorySucceeds()
        {
            var command = BuildCreateCommand();
            var repoResult = Result<DtoResponse>.Success(new DtoResponse
            {
                PaymentId = Guid.NewGuid(),
                BillingId = "pi_123",
                Status = "pending",
                Amount = command.Amount,
                ClientSecret = "pi_123_secret_abc"
            });

            var repo = new Mock<IPaymentRepository>();
            repo.Setup(r => r.CreateAbacatePayment(It.IsAny<CreateAbacatePaymentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(repoResult);

            var handler = new CreatePaymentCommandHandler(repo.Object, NullLogger<CreatePaymentCommandHandler>.Instance);

            var result = await handler.Handle(command, CancellationToken.None);

            result.HasSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value!.BillingId.Should().Be("pi_123");
            result.Value.ClientSecret.Should().Be("pi_123_secret_abc");
        }

        [Fact]
        public async Task CreatePayment_MapsCommandFieldsIntoRepositoryRequest()
        {
            var command = BuildCreateCommand();
            CreateAbacatePaymentRequest? captured = null;

            var repo = new Mock<IPaymentRepository>();
            repo.Setup(r => r.CreateAbacatePayment(It.IsAny<CreateAbacatePaymentRequest>(), It.IsAny<CancellationToken>()))
                .Callback<CreateAbacatePaymentRequest, CancellationToken>((req, _) => captured = req)
                .ReturnsAsync(Result<DtoResponse>.Success(new DtoResponse()));

            var handler = new CreatePaymentCommandHandler(repo.Object, NullLogger<CreatePaymentCommandHandler>.Instance);

            await handler.Handle(command, CancellationToken.None);

            captured.Should().NotBeNull();
            captured!.OrderId.Should().Be(command.OrderId);
            captured.Amount.Should().Be(command.Amount);
            captured.PaymentMethod.Should().Be("card");
            captured.CustomerEmail.Should().Be(command.CustomerEmail);
        }

        [Fact]
        public async Task CreatePayment_ReturnsError_WhenRepositoryFails()
        {
            var failed = new Result<DtoResponse>();
            failed.WithError("gateway recusou");

            var repo = new Mock<IPaymentRepository>();
            repo.Setup(r => r.CreateAbacatePayment(It.IsAny<CreateAbacatePaymentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failed);

            var handler = new CreatePaymentCommandHandler(repo.Object, NullLogger<CreatePaymentCommandHandler>.Instance);

            var result = await handler.Handle(BuildCreateCommand(), CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("gateway recusou");
        }

        [Fact]
        public async Task CreatePayment_ReturnsInternalError_WhenRepositoryThrows()
        {
            var repo = new Mock<IPaymentRepository>();
            repo.Setup(r => r.CreateAbacatePayment(It.IsAny<CreateAbacatePaymentRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));

            var handler = new CreatePaymentCommandHandler(repo.Object, NullLogger<CreatePaymentCommandHandler>.Instance);

            var result = await handler.Handle(BuildCreateCommand(), CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Internal error creating payment");
        }

        // ---------- CancelPaymentCommandHandler ----------

        [Fact]
        public async Task CancelPayment_ReturnsSuccess_WhenRepositorySucceeds()
        {
            var paymentId = Guid.NewGuid();

            var repo = new Mock<IPaymentRepository>();
            repo.Setup(r => r.CancelPayment(paymentId, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResultValue<bool>.Success(true));

            var handler = new CancelPaymentCommandHandler(repo.Object, NullLogger<CancelPaymentCommandHandler>.Instance);

            var result = await handler.Handle(new CancelPaymentCommand(paymentId), CancellationToken.None);

            result.HasSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
        }

        [Fact]
        public async Task CancelPayment_ReturnsError_WhenRepositoryFails()
        {
            var paymentId = Guid.NewGuid();
            var failed = new ResultValue<bool>();
            failed.WithError("pagamento já capturado");

            var repo = new Mock<IPaymentRepository>();
            repo.Setup(r => r.CancelPayment(paymentId, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(failed);

            var handler = new CancelPaymentCommandHandler(repo.Object, NullLogger<CancelPaymentCommandHandler>.Instance);

            var result = await handler.Handle(new CancelPaymentCommand(paymentId), CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("pagamento já capturado");
        }

        [Fact]
        public async Task CancelPayment_ReturnsInternalError_WhenRepositoryThrows()
        {
            var paymentId = Guid.NewGuid();

            var repo = new Mock<IPaymentRepository>();
            repo.Setup(r => r.CancelPayment(paymentId, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("db down"));

            var handler = new CancelPaymentCommandHandler(repo.Object, NullLogger<CancelPaymentCommandHandler>.Instance);

            var result = await handler.Handle(new CancelPaymentCommand(paymentId), CancellationToken.None);

            result.HasSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Internal error canceling payment");
        }

        [Fact]
        public async Task CancelPayment_PassesReasonToRepository()
        {
            var paymentId = Guid.NewGuid();
            string? capturedReason = null;

            var repo = new Mock<IPaymentRepository>();
            repo.Setup(r => r.CancelPayment(paymentId, It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, string, Guid, bool, CancellationToken>((_, reason, _, _, _) => capturedReason = reason)
                .ReturnsAsync(ResultValue<bool>.Success(true));

            var handler = new CancelPaymentCommandHandler(repo.Object, NullLogger<CancelPaymentCommandHandler>.Instance);

            await handler.Handle(new CancelPaymentCommand(paymentId, "cliente desistiu"), CancellationToken.None);

            capturedReason.Should().Be("cliente desistiu");
        }
    }
}
