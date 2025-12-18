using Microsoft.EntityFrameworkCore;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Entities;
using UnaProject.Domain.Enums;
using UnaProject.Domain.Helpers;
using UnaProject.Infra.Data;
using Microsoft.Extensions.Logging;
using UnaProject.Application.Models.Requests.Payments;
using CreateAbacatePaymentResponse = UnaProject.Application.Models.Dtos.CreateAbacatePaymentResponse;

namespace UnaProject.Infra.Repositories
{
    public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
    {
        private readonly AppDbContext _context;
        private readonly IAbacatePayService _abacatePayService;
        private readonly ILogger<PaymentRepository> _logger;

        public PaymentRepository(
            AppDbContext context,
            IAbacatePayService abacatePayService,
            ILogger<PaymentRepository> logger) : base(context)
        {
            _context = context;
            _abacatePayService = abacatePayService;
            _logger = logger;
        }

        public async Task<Result<CreateAbacatePaymentResponse>> CreateAbacatePayment(
            CreateAbacatePaymentRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating AbacatePay payment for order: {OrderId}", request.OrderId);

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

                // Create billing in AbacatePay
                var billingRequest = new CreateBillingRequest
                {
                    Amount = request.Amount,
                    Methods = [request.PaymentMethod.ToLowerInvariant()],
                    Customer = new CustomerRequest
                    {
                        Name = request.CustomerName,
                        Document = request.CustomerDocument,
                        Email = request.CustomerEmail,
                        Phone = request.CustomerPhone
                    },
                    Products = order.OrderItems.Select(item => new ProductRequest
                    {
                        Name = item.Product?.Name ?? "Product",
                        Quantity = item.Quantity,
                        Price = item.UnitPrice
                    }).ToList(),
                    ReturnUrl = request.ReturnUrl,
                    Metadata = request.Metadata
                };

                var billingResult = await _abacatePayService.CreateBillingAsync(billingRequest);

                if (!billingResult.HasSuccess)
                {
                    _logger.LogError("Failed to create billing: {Error}", billingResult.ErrorMessage);
                    var result = new Result<CreateAbacatePaymentResponse>();
                    result.WithError($"Error creating billing: {billingResult.ErrorMessage}");
                    return result;
                }

                var billing = billingResult.Value!;

                // Criar pagamento local
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    Status = "pending",
                    PaymentMethod = request.PaymentMethod.ToLowerInvariant(),
                    BillingId = billing.Id,
                    PaymentUrl = billing.Url,
                    QrCode = billing.QrCode,
                    QrCodeImage = billing.QrCodeImage,
                    ExpiresAt = billing.ExpiresAt,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DevMode = billing.DevMode,
                    AbacateStatus = billing.Status,
                    AbacateMethod = request.PaymentMethod,
                    Metadata = request.Metadata
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment created successfully: {PaymentId} - BillingId: {BillingId}",
                    payment.Id, billing.Id);

                var response = new CreateAbacatePaymentResponse
                {
                    PaymentId = payment.Id,
                    BillingId = billing.Id,
                    PaymentUrl = billing.Url,
                    QrCode = billing.QrCode,
                    QrCodeImage = billing.QrCodeImage,
                    Status = payment.Status,
                    ExpiresAt = billing.ExpiresAt,
                    Amount = payment.Amount,
                    DevMode = payment.DevMode ?? false
                };

                return Result<CreateAbacatePaymentResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating AbacatePay payment for order: {OrderId}", request.OrderId);
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
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing payment webhook for BillingId: {BillingId} - Status: {Status}",
                    billingId, status);

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.BillingId == billingId, cancellationToken);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for webhook BillingId: {BillingId}", billingId);
                    var result = new ResultValue<bool>();
                    result.WithNotFound("Payment not found");
                    return result;
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
                if (status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                {
                    var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);

                    if (order != null)
                    {
                        order.Status = "Paid";
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
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Cancelling payment: {PaymentId} - Reason: {Reason}", paymentId, reason);

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

                if (payment == null)
                {
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

                // Cancel on AbacatePay if BillingId exists
                bool cancelledOnGateway = false;
                if (!string.IsNullOrEmpty(payment.BillingId))
                {
                    var cancelResult = await _abacatePayService.CancelBillingAsync(payment.BillingId);

                    if (!cancelResult.HasSuccess)
                    {
                        _logger.LogError("Failed to cancel billing on AbacatePay: {BillingId} - {Error}",
                            payment.BillingId, cancelResult.ErrorMessage);
                        payment.ErrorMessage = $"Cancel on gateway failed: {cancelResult.ErrorMessage}";
                    }
                    else
                    {
                        cancelledOnGateway = true;
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
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting payment status for: {PaymentId}", paymentId);

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

                if (payment == null)
                {
                    _logger.LogWarning("Payment not found: {PaymentId}", paymentId);
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

                // Sync with AvocadoPay if pending.
                if (!string.IsNullOrEmpty(payment.BillingId) &&
                    payment.Status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var billingStatusResult = await _abacatePayService.GetBillingStatusAsync(payment.BillingId);

                        if (billingStatusResult.HasSuccess)
                        {
                            var billingStatus = billingStatusResult.Value!;

                            if (billingStatus.Status != payment.Status)
                            {
                                payment.Status = billingStatus.Status.ToLowerInvariant();
                                payment.UpdatedAt = DateTime.UtcNow;

                                if (billingStatus.PaidAt.HasValue)
                                {
                                    payment.ProcessedAt = billingStatus.PaidAt;
                                }

                                if (billingStatus.PlatformFee.HasValue)
                                {
                                    payment.Fee = billingStatus.PlatformFee;
                                }

                                await _context.SaveChangesAsync(cancellationToken);

                                statusDto.Status = payment.Status;
                                statusDto.UpdatedAt = payment.UpdatedAt;
                                statusDto.ProcessedAt = payment.ProcessedAt;
                                statusDto.Fee = payment.Fee;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to sync payment status with AbacatePay: {PaymentId}", payment.Id);
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
    }
}