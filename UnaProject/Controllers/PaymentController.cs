using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using UnaProject.Application.Commands.Payments;
using UnaProject.Application.Models.Requests.Payments;
using UnaProject.Application.Models.Dtos;
using UnaProject.Application.Queries.Payments;
using UnaProject.Domain.Helpers;

namespace UnaProject.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [SwaggerOperation(
            Summary = "Create new payment",
            Description = "Creates a new payment via PIX or credit card through AbacatePay")]
        [SwaggerResponse(200, "Payment created successfully", typeof(Result<CreateAbacatePaymentResponse>))]
        [SwaggerResponse(400, "Validation error")]
        [SwaggerResponse(500, "Internal server error")]
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreateAbacatePaymentRequest request)
        {
            var command = new CreateAbacatePaymentCommand(
                request.OrderId,
                request.Amount,
                request.PaymentMethod,
                request.CustomerName,
                request.CustomerDocument,
                request.CustomerEmail,
                request.CustomerPhone,
                request.ReturnUrl,
                request.Metadata
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
            var query = new GetPaymentStatusQuery(id);

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
            var command = new CancelAbacatePaymentCommand(id, request?.Reason ?? "Cancellation requested by user");

            var response = await _mediator.Send(command);

            if (response.HasSuccess)
                return Ok(response);

            return BadRequest(response);
        }

        [SwaggerOperation(
            Summary = "Process payment webhook",
            Description = "Endpoint to receive webhooks from AbacatePay about payment status changes")]
        [SwaggerResponse(200, "Webhook processed successfully")]
        [SwaggerResponse(400, "Error processing webhook")]
        [SwaggerResponse(500, "Internal server error")]
        [HttpPost("webhook")]
        public async Task<IActionResult> ProcessWebhook([FromBody] object payload)
        {
            try
            {
                // Extract signature from header
                var signature = Request.Headers["X-Signature"].FirstOrDefault();
                var webhookSecret = "your-webhook-secret"; // Should come from configuration

                var payloadString = System.Text.Json.JsonSerializer.Serialize(payload);

                var command = new ProcessPaymentWebhookCommand(payloadString, signature, webhookSecret,
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                var response = await _mediator.Send(command);

                if (response.HasSuccess)
                    return Ok(new { message = "Webhook processed successfully" });

                return BadRequest(new { error = response.ErrorMessage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal error processing webhook" });
            }
        }

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
            var query = new GetPaymentStatusQuery(id);
            var response = await _mediator.Send(query);

            if (response.HasSuccess && !string.IsNullOrEmpty(response.Value?.QrCode))
                return Ok(new { qrCode = response.Value.QrCode, paymentUrl = response.Value.PaymentUrl });

            return NotFound(new { error = "QR Code not available for this payment" });
        }
    }
}