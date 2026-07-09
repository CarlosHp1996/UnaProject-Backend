using Microsoft.EntityFrameworkCore;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Models.Dtos.Gateway;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Enums;
using UnaProject.Domain.Helpers;
using UnaProject.Infra.Data;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Models.Requests.Payments;
using UnaProject.Application.Models.Responses.Payments;
using CreateAbacatePaymentResponse = UnaProject.Application.Models.Dtos.CreateAbacatePaymentResponse;

namespace UnaProject.Infra.Repositories
{
    public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
    {
        private readonly AppDbContext _context;
        private readonly IPaymentGatewayResolver _paymentGatewayResolver;
        private readonly ILogger<PaymentRepository> _logger;

        public PaymentRepository(
            AppDbContext context,
            IPaymentGatewayResolver paymentGatewayResolver,
            ILogger<PaymentRepository> logger) : base(context)
        {
            _context = context;
            _paymentGatewayResolver = paymentGatewayResolver;
            _logger = logger;
        }

        public async Task<Result<CreateAbacatePaymentResponse>> CreateAbacatePayment(
            CreateAbacatePaymentRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating payment for order: {OrderId} via method {PaymentMethod}",
                    request.OrderId, request.PaymentMethod);

                // Validar pedido existe
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

                if (order == null)
                {
                    var result = new Result<CreateAbacatePaymentResponse>();
                    result.WithNotFound("Order not found");
                    return result;
                }

                // IDOR: um usuário não-admin só pode criar pagamento para pedido PRÓPRIO.
                // Retorna NotFound (não vaza existência do pedido de outro usuário).
                if (!request.IsAdmin && order.UserId != request.UserId)
                {
                    _logger.LogWarning("User {UserId} attempted to create payment for order {OrderId} owned by {OwnerId}",
                        request.UserId, order.Id, order.UserId);
                    var result = new Result<CreateAbacatePaymentResponse>();
                    result.WithNotFound("Order not found");
                    return result;
                }

                // O valor cobrado é SEMPRE o total do pedido (fonte da verdade no servidor),
                // nunca o valor enviado pelo cliente. Se o cliente mandar um Amount divergente, rejeita.
                var authoritativeAmount = order.TotalAmount;
                if (request.Amount > 0 && request.Amount != authoritativeAmount)
                {
                    _logger.LogWarning("Payment amount {RequestAmount} diverges from order total {OrderTotal} for order {OrderId}",
                        request.Amount, authoritativeAmount, order.Id);
                    var result = new Result<CreateAbacatePaymentResponse>();
                    result.WithError("Payment amount does not match the order total.");
                    return result;
                }

                IPaymentGateway gateway;
                try
                {
                    gateway = _paymentGatewayResolver.Resolve(request.PaymentMethod);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "No gateway configured for payment method: {PaymentMethod}", request.PaymentMethod);
                    var result = new Result<CreateAbacatePaymentResponse>();
                    result.WithError(ex.Message);
                    return result;
                }

                var gatewayRequest = new GatewayPaymentRequest
                {
                    Amount = authoritativeAmount,
                    Currency = "BRL",
                    OrderId = request.OrderId,
                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    CustomerDocument = request.CustomerDocument,
                    CustomerPhone = request.CustomerPhone,
                    PaymentMethod = request.PaymentMethod.ToLowerInvariant(),
                    ReturnUrl = request.ReturnUrl,
                    Metadata = string.IsNullOrEmpty(request.Metadata)
                        ? null
                        : new Dictionary<string, string> { ["info"] = request.Metadata }
                };

                var gatewayResult = await gateway.CreatePaymentAsync(gatewayRequest, cancellationToken);

                if (!gatewayResult.HasSuccess || gatewayResult.Value == null)
                {
                    _logger.LogError("Failed to create payment via {Provider}: {Error}",
                        gateway.ProviderName, gatewayResult.ErrorMessage);
                    var result = new Result<CreateAbacatePaymentResponse>();
                    result.WithError($"Error creating payment: {gatewayResult.ErrorMessage}");
                    return result;
                }

                var gatewayPayment = gatewayResult.Value;

                // Criar pagamento local. BillingId é preenchido com o ExternalId do gateway
                // para manter compatibilidade com as consultas existentes por BillingId
                // (ex.: GET api/payment/billing/{billingId}).
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    Amount = authoritativeAmount,
                    Currency = "BRL",
                    Status = "pending",
                    PaymentMethod = request.PaymentMethod.ToLowerInvariant(),
                    TransactionId = gatewayPayment.ExternalId,
                    ClientSecret = gatewayPayment.ClientSecret,
                    BillingId = gatewayPayment.ExternalId,
                    PaymentUrl = gatewayPayment.PaymentUrl,
                    QrCode = gatewayPayment.QrCode,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AbacateStatus = gatewayPayment.Status,
                    AbacateMethod = request.PaymentMethod,
                    Metadata = request.Metadata,
                    CustomerEmail = request.CustomerEmail
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment created successfully: {PaymentId} - Provider: {Provider} - ExternalId: {ExternalId}",
                    payment.Id, gateway.ProviderName, gatewayPayment.ExternalId);

                var response = new CreateAbacatePaymentResponse
                {
                    PaymentId = payment.Id,
                    BillingId = gatewayPayment.ExternalId,
                    PaymentUrl = gatewayPayment.PaymentUrl,
                    QrCode = gatewayPayment.QrCode,
                    ClientSecret = gatewayPayment.ClientSecret,
                    Status = payment.Status,
                    Amount = payment.Amount,
                    DevMode = false
                };

                return Result<CreateAbacatePaymentResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for order: {OrderId}", request.OrderId);
                var result = new Result<CreateAbacatePaymentResponse>();
                result.WithException("Internal error creating payment");
                return result;
            }
        }

        public async Task<ResultValue<bool>> ProcessPaymentWebhook(
            string billingId,
            string status,
            decimal? fee,
            DateTime? paidAt,
            string metadata,
            long? amountReceivedCents,
            string? currency,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing payment webhook for ExternalId: {BillingId} - Status: {Status}",
                    billingId, status);

                // billingId aqui é o identificador externo genérico (BillingId do AbacatePay
                // ou TransactionId/PaymentIntent.Id do Stripe) — buscamos por ambos os campos.
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TransactionId == billingId || p.BillingId == billingId, cancellationToken);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for webhook ExternalId: {BillingId}", billingId);
                    var result = new ResultValue<bool>();
                    result.WithNotFound("Payment not found");
                    return result;
                }

                var isPaid = status.Equals("paid", StringComparison.OrdinalIgnoreCase)
                          || status.Equals("succeeded", StringComparison.OrdinalIgnoreCase);

                // A-2: antes de marcar como PAGO, conferir valor recebido e moeda contra o esperado.
                // Se divergir, NÃO marca pago — sinaliza para revisão manual e não libera o pedido.
                if (isPaid)
                {
                    var expectedCents = (long)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
                    var expectedCurrency = string.IsNullOrWhiteSpace(payment.Currency) ? "brl" : payment.Currency.ToLowerInvariant();
                    var receivedCurrency = currency?.ToLowerInvariant();

                    var amountMismatch = amountReceivedCents.HasValue && amountReceivedCents.Value != expectedCents;
                    var currencyMismatch = !string.IsNullOrEmpty(receivedCurrency) && receivedCurrency != expectedCurrency;

                    if (amountMismatch || currencyMismatch)
                    {
                        _logger.LogError(
                            "Webhook amount/currency mismatch for Payment {PaymentId}. Expected {ExpectedCents} {ExpectedCurrency}, received {ReceivedCents} {ReceivedCurrency}. Marking for manual review; order NOT released.",
                            payment.Id, expectedCents, expectedCurrency, amountReceivedCents, receivedCurrency);

                        payment.Status = "review";
                        payment.UpdatedAt = DateTime.UtcNow;
                        payment.ErrorMessage = "Amount/currency mismatch on webhook - manual review required";
                        if (!string.IsNullOrEmpty(metadata))
                            payment.Metadata = metadata;

                        await _context.SaveChangesAsync(cancellationToken);

                        var mismatchResult = new ResultValue<bool>();
                        mismatchResult.WithError("Webhook amount/currency mismatch - payment held for manual review");
                        return mismatchResult;
                    }
                }

                // Update payment data
                payment.Status = status.ToLowerInvariant();
                payment.UpdatedAt = DateTime.UtcNow;

                if (paidAt.HasValue)
                    payment.ProcessedAt = paidAt;

                if (fee.HasValue)
                    payment.Fee = fee;

                if (!string.IsNullOrEmpty(metadata))
                    payment.Metadata = metadata;

                // Update order status if approved.
                if (isPaid)
                {
                    var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);

                    if (order != null)
                    {
                        // Correção de bucket (Fase 3): "Paid" é status de PAGAMENTO, não de fulfillment.
                        // Antes gravava order.Status = "Paid" (bucket errado); agora vai em PaymentStatus.
                        order.PaymentStatus = EnumPaymentStatus.Paid;
                        order.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment webhook processed successfully: {PaymentId} - {Status}",
                    payment.Id, status);

                return ResultValue<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment webhook for BillingId: {BillingId}", billingId);
                var result = new ResultValue<bool>();
                result.WithException("Error processing webhook");
                return result;
            }
        }

        public async Task<ResultValue<bool>> CancelPayment(
            Guid paymentId,
            string reason,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Cancelling payment: {PaymentId} - Reason: {Reason}", paymentId, reason);

                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

                if (payment == null)
                {
                    var result = new ResultValue<bool>();
                    result.WithNotFound("Payment not found");
                    return result;
                }

                // IDOR: só o dono do pedido (ou Admin) pode cancelar. Não vaza existência.
                if (!isAdmin && payment.Order?.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to cancel payment {PaymentId} they do not own", userId, paymentId);
                    var result = new ResultValue<bool>();
                    result.WithNotFound("Payment not found");
                    return result;
                }

                if (payment.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                {
                    var result = new ResultValue<bool>();
                    result.WithError("Cannot cancel a payment that has already been approved");
                    return result;
                }

                if (payment.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    var result = new ResultValue<bool>();
                    result.WithError("Payment has already been cancelled");
                    return result;
                }

                // Cancel on the payment gateway if an external id exists
                bool cancelledOnGateway = false;
                var externalId = !string.IsNullOrEmpty(payment.TransactionId) ? payment.TransactionId : payment.BillingId;
                if (!string.IsNullOrEmpty(externalId))
                {
                    try
                    {
                        var gateway = _paymentGatewayResolver.Resolve(payment.PaymentMethod);
                        var cancelResult = await gateway.CancelAsync(externalId, reason, cancellationToken);

                        if (!cancelResult.HasSuccess)
                        {
                            _logger.LogError("Failed to cancel payment on gateway: {ExternalId} - {Error}",
                                externalId, cancelResult.ErrorMessage);
                            payment.ErrorMessage = $"Cancel on gateway failed: {cancelResult.ErrorMessage}";
                        }
                        else
                        {
                            cancelledOnGateway = true;
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(ex, "No gateway configured to cancel payment method {PaymentMethod}", payment.PaymentMethod);
                        payment.ErrorMessage = ex.Message;
                    }
                }

                // Update local status
                payment.Status = "cancelled";
                payment.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(reason))
                {
                    var existingMetadata = payment.Metadata ?? "";
                    payment.Metadata = string.IsNullOrEmpty(existingMetadata)
                        ? $"Cancel reason: {reason}"
                        : $"{existingMetadata}; Cancel reason: {reason}";
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment cancelled successfully: {PaymentId} - Gateway cancelled: {CancelledOnGateway}",
                    payment.Id, cancelledOnGateway);

                return ResultValue<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment: {PaymentId}", paymentId);
                var result = new ResultValue<bool>();
                result.WithException("Error cancelling payment");
                return result;
            }
        }

        public async Task<Result<PaymentStatusDto>> GetPaymentStatus(
            Guid paymentId,
            Guid userId,
            bool isAdmin,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting payment status for: {PaymentId}", paymentId);

                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found: {PaymentId}", paymentId);
                    var result = new Result<PaymentStatusDto>();
                    result.WithNotFound("Payment not found");
                    return result;
                }

                // IDOR: só o dono do pedido (ou Admin) pode consultar. Não vaza existência.
                if (!isAdmin && payment.Order?.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to read payment {PaymentId} they do not own", userId, paymentId);
                    var result = new Result<PaymentStatusDto>();
                    result.WithNotFound("Payment not found");
                    return result;
                }

                var statusDto = new PaymentStatusDto
                {
                    PaymentId = payment.Id,
                    BillingId = payment.BillingId,
                    Status = payment.Status,
                    PaymentMethod = payment.PaymentMethod,
                    Amount = payment.Amount,
                    Fee = payment.Fee,
                    PaymentUrl = payment.PaymentUrl,
                    QrCode = payment.QrCode,
                    ExpiresAt = payment.ExpiresAt,
                    CreatedAt = payment.CreatedAt,
                    UpdatedAt = payment.UpdatedAt ?? DateTime.UtcNow,
                    ProcessedAt = payment.ProcessedAt,
                    ErrorMessage = payment.ErrorMessage,
                    DevMode = payment.DevMode ?? false
                };

                // Parse AvocadoStatus if available
                if (!string.IsNullOrEmpty(payment.AbacateStatus) &&
                    Enum.TryParse<PaymentStatusAbacate>(payment.AbacateStatus, out var abacateStatus))
                {
                    statusDto.AbacateStatus = abacateStatus;
                }

                // Sync with the payment gateway if pending.
                var externalIdForSync = !string.IsNullOrEmpty(payment.TransactionId) ? payment.TransactionId : payment.BillingId;
                if (!string.IsNullOrEmpty(externalIdForSync) &&
                    payment.Status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var gateway = _paymentGatewayResolver.Resolve(payment.PaymentMethod);
                        var gatewayStatusResult = await gateway.GetStatusAsync(externalIdForSync, cancellationToken);

                        if (gatewayStatusResult.HasSuccess && gatewayStatusResult.Value != null)
                        {
                            var gatewayStatus = gatewayStatusResult.Value;

                            if (gatewayStatus.Status != payment.Status)
                            {
                                payment.Status = gatewayStatus.Status.ToLowerInvariant();
                                payment.UpdatedAt = DateTime.UtcNow;

                                if (gatewayStatus.PaidAt.HasValue)
                                {
                                    payment.ProcessedAt = gatewayStatus.PaidAt;
                                }

                                await _context.SaveChangesAsync(cancellationToken);

                                statusDto.Status = payment.Status;
                                statusDto.UpdatedAt = payment.UpdatedAt;
                                statusDto.ProcessedAt = payment.ProcessedAt;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to sync payment status with gateway: {PaymentId}", payment.Id);
                    }
                }

                return Result<PaymentStatusDto>.Success(statusDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status: {PaymentId}", paymentId);
                var result = new Result<PaymentStatusDto>();
                result.WithException("Error getting payment status");
                return result;
            }
        }

        public async Task<Result<PaymentDto>> GetPaymentByBillingId(
            string billingId,
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(billingId))
                {
                    var result = new Result<PaymentDto>();
                    result.WithError("BillingId é obrigatório");
                    return result;
                }

                _logger.LogInformation("Getting payment by BillingId: {BillingId}", billingId);

                var payment = await _context.Payments
                    .Include(p => p.Order)
                    .FirstOrDefaultAsync(p => p.BillingId == billingId, cancellationToken);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for BillingId: {BillingId}", billingId);
                    var result = new Result<PaymentDto>();
                    result.WithNotFound("Payment not found");
                    return result;
                }

                var paymentDto = new PaymentDto
                {
                    Id = payment.Id,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    Fee = payment.Fee,
                    Status = payment.Status,
                    PaymentMethod = payment.PaymentMethod,
                    BillingId = payment.BillingId,
                    PaymentUrl = payment.PaymentUrl,
                    QrCode = payment.QrCode,
                    ExpiresAt = payment.ExpiresAt,
                    CreatedAt = payment.CreatedAt,
                    UpdatedAt = payment.UpdatedAt ?? DateTime.UtcNow,
                    ProcessedAt = payment.ProcessedAt,
                    ErrorMessage = payment.ErrorMessage,
                    DevMode = payment.DevMode ?? false,
                    AbacateStatus = payment.AbacateStatus,
                    AbacateFrequency = payment.AbacateFrequency,
                    AbacateMethod = payment.AbacateMethod,
                    AbacateFeeType = payment.AbacateFeeType,
                    Metadata = payment.Metadata
                };

                return Result<PaymentDto>.Success(paymentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment by BillingId: {BillingId}", billingId);
                var result = new Result<PaymentDto>();
                result.WithException("Error getting payment");
                return result;
            }
        }

        public async Task<Result<List<PaymentDto>>> GetPaymentsByOrder(
            Guid orderId,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting payments for order: {OrderId}", orderId);

                var payments = await _context.Payments
                    .Where(p => p.OrderId == orderId)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);

                var paymentDtos = payments.Select(payment => new PaymentDto
                {
                    Id = payment.Id,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    Fee = payment.Fee,
                    Status = payment.Status,
                    PaymentMethod = payment.PaymentMethod,
                    BillingId = payment.BillingId,
                    PaymentUrl = payment.PaymentUrl,
                    QrCode = payment.QrCode,
                    ExpiresAt = payment.ExpiresAt,
                    CreatedAt = payment.CreatedAt,
                    UpdatedAt = payment.UpdatedAt ?? DateTime.UtcNow,
                    ProcessedAt = payment.ProcessedAt,
                    ErrorMessage = payment.ErrorMessage,
                    DevMode = payment.DevMode ?? false,
                    AbacateStatus = payment.AbacateStatus,
                    AbacateFrequency = payment.AbacateFrequency,
                    AbacateMethod = payment.AbacateMethod,
                    AbacateFeeType = payment.AbacateFeeType,
                    Metadata = payment.Metadata
                }).ToList();

                return Result<List<PaymentDto>>.Success(paymentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payments for order: {OrderId}", orderId);
                var result = new Result<List<PaymentDto>>();
                result.WithException("Error getting payments for order");
                return result;
            }
        }

        public async Task<PaymentStatisticsResponse> GetPaymentStatistics(Guid? userId, CancellationToken cancellationToken)
        {
            var query = _context.Payments.AsQueryable();

            if (userId.HasValue)
                query = query.Where(p => p.Order.UserId == userId.Value);

            var payments = await query.ToListAsync(cancellationToken);

            var stats = new PaymentStatisticsResponse
            {
                TotalPayments = payments.Count,
                SuccessfulPayments = payments.Count(p => p.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("succeeded", StringComparison.OrdinalIgnoreCase)),
                FailedPayments = payments.Count(p => p.Status.Equals("failed", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("error", StringComparison.OrdinalIgnoreCase)),
                PendingPayments = payments.Count(p => p.Status.Equals("pending", StringComparison.OrdinalIgnoreCase)),
                TotalAmount = payments.Where(p => p.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("succeeded", StringComparison.OrdinalIgnoreCase)).Sum(p => p.Amount),
                AveragePayment = payments.Any(p => p.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("succeeded", StringComparison.OrdinalIgnoreCase))
                    ? payments.Where(p => p.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) || p.Status.Equals("succeeded", StringComparison.OrdinalIgnoreCase)).Average(p => p.Amount)
                    : 0
            };

            return stats;
        }
    }
}