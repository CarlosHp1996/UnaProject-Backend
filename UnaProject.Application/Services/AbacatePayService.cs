using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UnaProject.Application.Models.Requests.Payments;
using UnaProject.Application.Models.Responses.Payments;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services
{
    public class AbacatePayService : IAbacatePayService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AbacatePayOptions _options;
        private readonly ILogger<AbacatePayService> _logger;

        // AbacatePay's HMAC public key for webhook validation.
        private const string ABACATEPAY_PUBLIC_KEY =
            "t9dXRhHHo3yDEj5pVDYz0frf7q6bMKyMRmxxCPIPp3RCplBfXRxqlC6ZpiWmOqj4L63qEaeUOtrCI8P0VMUgo6iIga2ri9ogaHFs0WIIywSMg0q7RmBfybe1E5XJcfC4IW3alNqym0tXoAKkzvfEjZxV6bE0oG2zJrNNYmUCKZyV0KZ3JS8Votf9EAWWYdiDkMkpbMdPggfh1EqHlVkMiTady6jOR3hyzGEHrIz2Ret0xHKMbiqkr9HS1JhNH";

        public AbacatePayService(
            IHttpClientFactory httpClientFactory,
            IOptionsMonitor<AbacatePayOptions> options,
            ILogger<AbacatePayService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.CurrentValue;
            _logger = logger;
        }

        public async Task<Result<CreateBillingResponse>> CreateBillingAsync(CreateBillingRequest request)
        {
            try
            {
                _logger.LogInformation("Creating billing for amount: {Amount}", request.Amount);

                var httpClient = _httpClientFactory.CreateClient("AbacatePay");

                var payload = new
                {
                    frequency = "ONE_TIME",
                    methods = request.Methods?.Select(m => m.ToString()).ToList(),
                    products = request.Products?.Select(p => new
                    {
                        externalId = p.ExternalId,
                        name = p.Name,
                        quantity = p.Quantity,
                        price = p.Price,
                        description = p.Description
                    }).ToList(),
                    customer = new
                    {
                        email = request.Customer?.Email,
                        name = request.Customer?.Name,
                        document = request.Customer?.Document,
                        phone = request.Customer?.Phone
                    },
                    returnUrl = request.ReturnUrl,
                    metadata = request.Metadata
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("/v1/billing", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper<CreateBillingResponse>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (apiResponse?.Data != null)
                    {
                        _logger.LogInformation("Billing created successfully: {BillingId}", apiResponse.Data.Id);
                        return Result<CreateBillingResponse>.Success(apiResponse.Data);
                    }

                    var result = new Result<CreateBillingResponse>();
                    result.WithError("Invalid API response");
                    return result;
                }

                _logger.LogError("Failed to create billing: {StatusCode} - {Content}", response.StatusCode, responseContent);
                var errorResult = new Result<CreateBillingResponse>();
                errorResult.WithError($"API error: {response.StatusCode}");
                return errorResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating billing");
                var result = new Result<CreateBillingResponse>();
                result.WithException("Internal error while creating billing");
                return result;
            }
        }

        public async Task<Result<BillingStatusResponse>> GetBillingStatusAsync(string billingId)
        {
            try
            {
                _logger.LogInformation("Getting billing status: {BillingId}", billingId);

                var httpClient = _httpClientFactory.CreateClient("AbacatePay");

                var response = await httpClient.GetAsync($"/v1/billing/{billingId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<ApiResponseWrapper<BillingStatusResponse>>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (apiResponse?.Data != null)
                    {
                        _logger.LogInformation("Billing status retrieved: {Status}", apiResponse.Data.Status);
                        return Result<BillingStatusResponse>.Success(apiResponse.Data);
                    }

                    var result = new Result<BillingStatusResponse>();
                    result.WithError("Invalid API response");
                    return result;
                }

                _logger.LogError("Failed to get billing status: {StatusCode} - {Content}", response.StatusCode, responseContent);
                var errorResult = new Result<BillingStatusResponse>();
                errorResult.WithError($"API error: {response.StatusCode}");
                return errorResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing status: {BillingId}", billingId);
                var result = new Result<BillingStatusResponse>();
                result.WithException("Internal error while retrieving status");
                return result;
            }
        }

        public async Task<ResultValue<bool>> CancelBillingAsync(string billingId)
        {
            try
            {
                _logger.LogInformation("Cancelling billing: {BillingId}", billingId);

                var httpClient = _httpClientFactory.CreateClient("AbacatePay");

                var response = await httpClient.DeleteAsync($"/v1/billing/{billingId}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Billing cancelled successfully: {BillingId}", billingId);
                    return ResultValue<bool>.Success(true);
                }

                _logger.LogError("Failed to cancel billing: {StatusCode}", response.StatusCode);
                var result = new ResultValue<bool>();
                result.WithError($"Error cancelling: {response.StatusCode}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling billing: {BillingId}", billingId);
                var result = new ResultValue<bool>();
                result.WithException("Internal error while cancelling billing");
                return result;
            }
        }

        public async Task<ResultValue<bool>> ValidateWebhookSignatureAsync(string payload, string signature)
        {
            try
            {
                if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(signature))
                {
                    var result = new ResultValue<bool>();
                    result.WithError("Payload or signature not provided");
                    return result;
                }

                // Implement HMAC validation using AbacatePay's public key
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret ?? ABACATEPAY_PUBLIC_KEY));
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var computedSignature = Convert.ToHexString(computedHash).ToLower();

                var isValid = signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);

                _logger.LogInformation("Webhook signature validation: {IsValid}", isValid);

                return ResultValue<bool>.Success(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating webhook signature");
                var result = new ResultValue<bool>();
                result.WithException("Internal error while validating signature");
                return result;
            }
        }

        public async Task<ResultValue<bool>> ProcessWebhookEventAsync(string webhookPayload)
        {
            try
            {
                _logger.LogInformation("Processing webhook event");

                // Deserializar payload
                var webhookEvent = JsonSerializer.Deserialize<JsonElement>(webhookPayload);

                if (!webhookEvent.TryGetProperty("event", out var eventProperty))
                {
                    var result = new ResultValue<bool>();
                    result.WithError("Event not found in payload");
                    return result;
                }

                var eventType = eventProperty.GetString();
                _logger.LogInformation("Processing webhook event type: {EventType}", eventType);

                // Specific logic for different types of events will be added here.
                // For example: billing.paid, billing.cancelled, etc.

                return ResultValue<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook event");
                var result = new ResultValue<bool>();
                result.WithException("Internal error while processing webhook event");
                return result;
            }
        }

        public async Task<ResultValue<bool>> IsBillingValidAsync(string billingId)
        {
            try
            {
                var statusResult = await GetBillingStatusAsync(billingId);

                if (!statusResult.HasSuccess)
                {
                    var result = new ResultValue<bool>();
                    result.WithError(statusResult.ErrorMessage ?? "Error checking status");
                    return result;
                }

                var status = statusResult.Value;
                var isValid = status != null && !string.Equals(status.Status, "cancelled", StringComparison.OrdinalIgnoreCase);

                return ResultValue<bool>.Success(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating billing: {BillingId}", billingId);
                var result = new ResultValue<bool>();
                result.WithException("Internal error while validating billing");
                return result;
            }
        }

        private class ApiResponseWrapper<T>
        {
            public T? Data { get; set; }
            public string? Error { get; set; }
        }
    }
}