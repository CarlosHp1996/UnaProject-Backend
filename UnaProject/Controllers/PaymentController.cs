using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UnaProject.Application.Commands.Payments;
using UnaProject.Application.Interfaces;
using UnaProject.Application.Models.Requests.Payments;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Queries.Payments;
using UnaProject.Domain.Helpers;
using UnaProject.Application.Models.Responses.Payments;
using CreateAbacatePaymentResponse = UnaProject.Application.Models.Dtos.CreateAbacatePaymentResponse;

namespace UnaProject.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;
        private readonly IPaymentGatewayResolver _paymentGatewayResolver;

        public PaymentController(IMediator mediator, IConfiguration configuration, IPaymentGatewayResolver paymentGatewayResolver)
        {
            _mediator = mediator;
            _configuration = configuration;
            _paymentGatewayResolver = paymentGatewayResolver;
        }

        [SwaggerOperation(
            Summary = "Create new payment",
            Description = "Creates a new payment via PIX or credit card through Stripe")]
        [SwaggerResponse(200, "Payment created successfully", typeof(Result<CreateAbacatePaymentResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(500, "Internal server error")]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreateAbacatePaymentRequest request)
        {
            var userId = GetAuthenticatedUserId();
            if (userId == Guid.Empty && !IsAdmin())
                return Unauthorized(new { error = "Invalid user token" });

            var command = new CreatePaymentCommand(
                request.OrderId,
                request.Amount,
                request.PaymentMethod,
                request.CustomerName,
                request.CustomerDocument,
                request.CustomerEmail,
                request.CustomerPhone,
                request.ReturnUrl,
                request.Metadata,
                userId,
                IsAdmin()
            );

            var response = await _mediator.Send(command);

            if (response.HasSuccess)
                return Ok(response);

            return BadRequest(response);
        }

        [SwaggerOperation(
            Summary = "Get payment status",
            Description = "Retrieves the current status of a payment")]
        [SwaggerResponse(200, "Payment status retrieved successfully", typeof(Result<PaymentStatusDto>))]
        [SwaggerResponse(404, "Payment not found")]
        [SwaggerResponse(500, "Internal server error")]
        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetPaymentStatus(Guid id)
        {
            var query = new GetPaymentStatusQuery(id, GetAuthenticatedUserId(), IsAdmin());

            var response = await _mediator.Send(query);

            if (response.HasSuccess)
                return Ok(response);

            return BadRequest(response);
        }

        [SwaggerOperation(
            Summary = "Cancel payment",
            Description = "Cancels a pending payment")]
        [SwaggerResponse(200, "Payment canceled successfully", typeof(ResultValue<bool>))]
        [SwaggerResponse(400, "Validation error or payment already processed")]
        [SwaggerResponse(404, "Payment not found")]
        [SwaggerResponse(500, "Internal server error")]
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelPayment(Guid id, [FromBody] CancelPaymentRequest? request)
        {
            var command = new CancelPaymentCommand(
                id,
                request?.Reason ?? "Cancellation requested by user",
                GetAuthenticatedUserId(),
                IsAdmin());

            var response = await _mediator.Send(command);

            if (response.HasSuccess)
                return Ok(response);

            return BadRequest(response);
        }

        [SwaggerOperation(
            Summary = "Process payment webhook",
            Description = "Endpoint to receive webhooks from the payment gateway (Stripe) about payment status changes")]
        [SwaggerResponse(200, "Webhook processed successfully")]
        [SwaggerResponse(400, "Error processing webhook")]
        [SwaggerResponse(500, "Internal server error")]
        [AllowAnonymous]
        [HttpPost("webhook/{provider}")]
        public async Task<IActionResult> ProcessWebhook(string provider)
        {
            try
            {
                // Lê o corpo RAW da requisição ANTES de qualquer parse JSON — necessário para
                // validar a assinatura HMAC do Stripe (Stripe-Signature), que é calculada sobre os bytes originais.
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();

                var signature = Request.Headers["Stripe-Signature"].FirstOrDefault()
                    ?? Request.Headers["X-Signature"].FirstOrDefault();

                IPaymentGateway gateway;
                try
                {
                    gateway = _paymentGatewayResolver.ResolveByProvider(provider);
                }
                catch (InvalidOperationException)
                {
                    return BadRequest(new { error = $"Unknown payment provider: {provider}" });
                }

                var webhookSecret = provider.Equals("stripe", StringComparison.OrdinalIgnoreCase)
                    ? _configuration["Stripe:STRIPE_WEBHOOK_SECRET"]
                    : _configuration["AbacatePay:WebhookSecret"];

                var signatureValidation = await gateway.ValidateWebhookSignatureAsync(rawBody, signature, webhookSecret);
                if (!signatureValidation.HasSuccess || signatureValidation.Value != true)
                    return BadRequest(new { error = "Invalid webhook signature" });

                var parseResult = await gateway.ParseWebhookEventAsync(rawBody);
                if (!parseResult.HasSuccess || parseResult.Value == null)
                    return BadRequest(new { error = "Invalid webhook payload" });

                var command = new ProcessPaymentWebhookCommand(provider, rawBody, signature, webhookSecret,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                var response = await _mediator.Send(command);

                if (response.HasSuccess)
                    return Ok(new { message = "Webhook processed successfully" });

                return BadRequest(new { error = response.ErrorMessage });
            }
            catch (Exception)
            {
                return StatusCode(500, new { error = "Internal error processing webhook" });
            }
        }

        [SwaggerOperation(
            Summary = "Process payment webhook (legacy route)",
            Description = "Rota legada mantida por compatibilidade; assume o provedor 'stripe'.")]
        [AllowAnonymous]
        [HttpPost("webhook")]
        public Task<IActionResult> ProcessWebhookLegacy() => ProcessWebhook("stripe");

        [SwaggerOperation(
            Summary = "Get payment by BillingId",
            Description = "Retrieve a payment using the BillingId from AbacatePay")]
        [SwaggerResponse(200, "Payment found", typeof(Result<PaymentDto>))]
        [SwaggerResponse(404, "Payment not found")]
        [SwaggerResponse(500, "Internal server error")]
        [HttpGet("billing/{billingId}")]
        public async Task<IActionResult> GetPaymentByBillingId(string billingId)
        {
            var query = new GetPaymentByBillingIdQuery(billingId);

            var response = await _mediator.Send(query);

            if (response.HasSuccess)
                return Ok(response);

            return BadRequest(response);
        }

        [SwaggerOperation(
            Summary = "View payments for an order",
            Description = "Lists all payments associated with a specific order")]
        [SwaggerResponse(200, "List of payments", typeof(Result<List<PaymentDto>>))]
        [SwaggerResponse(404, "Order not found")]
        [SwaggerResponse(500, "Internal server error")]
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetPaymentsByOrder(Guid orderId)
        {
            var query = new GetPaymentsByOrderQuery(orderId);

            var response = await _mediator.Send(query);

            if (response.HasSuccess)
                return Ok(response);

            return BadRequest(response);
        }

        [SwaggerOperation(
            Summary = "Get payment QR Code",
            Description = "Returns the PIX QR Code of a payment for display")]
        [SwaggerResponse(200, "QR Code obtained successfully")]
        [SwaggerResponse(404, "Payment not found")]
        [SwaggerResponse(500, "Internal server error")]
        [HttpGet("{id}/qrcode")]
        public async Task<IActionResult> GetPaymentQrCode(Guid id)
        {
            var query = new GetPaymentStatusQuery(id, GetAuthenticatedUserId(), IsAdmin());
            var response = await _mediator.Send(query);

            if (response.HasSuccess && !string.IsNullOrEmpty(response.Value?.QrCode))
                return Ok(new { qrCode = response.Value.QrCode, paymentUrl = response.Value.PaymentUrl });

            return NotFound(new { error = "QR Code not available for this payment" });
        }

        [SwaggerOperation(
            Summary = "Get payment statistics",
            Description = "Retrieves payment statistics (admin sees all, user sees own)")]
        [SwaggerResponse(200, "Statistics retrieved successfully", typeof(Result<PaymentStatisticsResponse>))]
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            Guid? userId = null;
            if (!IsAdmin())
            {
                var id = GetAuthenticatedUserId();
                if (id != Guid.Empty)
                    userId = id;
            }

            var query = new GetPaymentStatisticsQuery(userId);
            var response = await _mediator.Send(query);

            if (response.HasSuccess)
                return Ok(response);

            return BadRequest(response);
        }

        /// <summary>
        /// Extrai o Id do usuário autenticado das claims do JWT.
        /// Tenta NameIdentifier (mapeado de "sub"), depois "sub" e "id" como fallback.
        /// </summary>
        private Guid GetAuthenticatedUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value;

            return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
        }

        private bool IsAdmin() => User.IsInRole("Admin");
    }
}