using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services
{
    public class PaymentNotificationService : IPaymentNotificationService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<PaymentNotificationService> _logger;
        private readonly NotificationOptions _options;

        public PaymentNotificationService(
            IEmailService emailService,
            IOptions<NotificationOptions> options,
            ILogger<PaymentNotificationService> logger)
        {
            _emailService = emailService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<Result<bool>> SendPaymentCreatedNotificationAsync(Guid paymentId, string customerEmail)
        {
            try
            {
                var subject = "Payment Created - Awaiting Confirmation";
                var htmlBody = await GeneratePaymentCreatedEmailTemplate(paymentId);

                await _emailService.SendEmailAsync(customerEmail, subject, htmlBody);

                _logger.LogInformation(
                    "Payment created notification sent successfully. PaymentId: {PaymentId}, Email: {Email}",
                    paymentId, customerEmail);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending payment created notification. PaymentId: {PaymentId}, Email: {Email}",
                    paymentId, customerEmail);

                return Result<bool>.Failure($"Error sending notification: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SendPaymentConfirmedNotificationAsync(Guid paymentId, string customerEmail)
        {
            try
            {
                var subject = "🎉 Payment Confirmed - Order Approved!";
                var htmlBody = await GeneratePaymentConfirmedEmailTemplate(paymentId);

                await _emailService.SendEmailAsync(customerEmail, subject, htmlBody);

                _logger.LogInformation(
                    "Payment confirmed notification sent successfully. PaymentId: {PaymentId}, Email: {Email}",
                    paymentId, customerEmail);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending payment confirmed notification. PaymentId: {PaymentId}, Email: {Email}",
                    paymentId, customerEmail);

                return Result<bool>.Failure($"Error sending confirmation: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SendPaymentCancelledNotificationAsync(Guid paymentId, string customerEmail)
        {
            try
            {
                var subject = "Payment Cancelled";
                var htmlBody = await GeneratePaymentCancelledEmailTemplate(paymentId);

                await _emailService.SendEmailAsync(customerEmail, subject, htmlBody);

                _logger.LogInformation(
                    "Payment cancelled notification sent successfully. PaymentId: {PaymentId}, Email: {Email}",
                    paymentId, customerEmail);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending payment cancelled notification. PaymentId: {PaymentId}, Email: {Email}",
                    paymentId, customerEmail);

                return Result<bool>.Failure($"Error sending cancellation: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SendPaymentExpiredNotificationAsync(Guid paymentId, string customerEmail)
        {
            try
            {
                var subject = "Payment Expired - New Attempt Available";
                var htmlBody = await GeneratePaymentExpiredEmailTemplate(paymentId);

                await _emailService.SendEmailAsync(customerEmail, subject, htmlBody);

                _logger.LogInformation(
                    "Payment expired notification sent successfully. PaymentId: {PaymentId}, Email: {Email}",
                    paymentId, customerEmail);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending payment expired notification. PaymentId: {PaymentId}, Email: {Email}",
                    paymentId, customerEmail);

                return Result<bool>.Failure($"Error sending expiration notification: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SendPaymentFailedAdminNotificationAsync(Guid paymentId, string errorMessage)
        {
            try
            {
                var subject = "Payment Processing Failed";
                var htmlBody = await GeneratePaymentFailedAdminEmailTemplate(paymentId, errorMessage);

                await _emailService.SendEmailAsync(_options.AdminEmail, subject, htmlBody);

                _logger.LogInformation(
                    "Payment failed admin notification sent successfully. PaymentId: {PaymentId}",
                    paymentId);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending payment failed admin notification. PaymentId: {PaymentId}",
                    paymentId);

                return Result<bool>.Failure($"Error notifying admin: {ex.Message}");
            }
        }

        public async Task<Result<bool>> SendWebhookFailedAdminNotificationAsync(Guid paymentId, string webhookEvent, string errorMessage, int attemptCount)
        {
            try
            {
                var subject = $"Webhook Failed - Attempt {attemptCount}";
                var htmlBody = await GenerateWebhookFailedAdminEmailTemplate(paymentId, webhookEvent, errorMessage, attemptCount);

                await _emailService.SendEmailAsync(_options.AdminEmail, subject, htmlBody);

                _logger.LogInformation(
                    "Webhook failed admin notification sent successfully. PaymentId: {PaymentId}, Event: {Event}, Attempt: {Attempt}",
                    paymentId, webhookEvent, attemptCount);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending webhook failed admin notification. PaymentId: {PaymentId}, Event: {Event}",
                    paymentId, webhookEvent);

                return Result<bool>.Failure($"Error notifying webhook failure: {ex.Message}");
            }
        }

        #region Email Template Generation

        private async Task<string> GeneratePaymentCreatedEmailTemplate(Guid paymentId)
        {
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #f8f9fa; padding: 20px; text-align: center;'>
                    <h2 style='color: #007bff;'>Payment Created Successfully! 📋</h2>
                </div>
                <div style='padding: 20px;'>
                    <p>Hello!</p>
                    <p>Your payment has been created successfully and is awaiting confirmation.</p>
                    <p><strong>Payment ID:</strong> {paymentId}</p>
                    <p>You will receive a new notification once the payment is confirmed.</p>
                    <p>If you have any questions, please contact us.</p>
                    <hr style='margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Una Estúdio Criativo - Handmade Products</p>
                </div>
            </body>
            </html>";
        }

        private async Task<string> GeneratePaymentConfirmedEmailTemplate(Guid paymentId)
        {
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #d4edda; padding: 20px; text-align: center;'>
                    <h2 style='color: #155724;'>Payment Confirmed!</h2>
                </div>
                <div style='padding: 20px;'>
                    <p>Congratulations!</p>
                    <p>Your payment has been <strong>successfully confirmed</strong>!</p>
                    <p><strong>Payment ID:</strong> {paymentId}</p>
                    <p>Your order is already being processed and you will receive shipping updates soon.</p>
                    <p>Thank you for choosing our handmade products!</p>
                    <hr style='margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Una Estúdio Criativo - Handmade Products</p>
                </div>
            </body>
            </html>";
        }

        private async Task<string> GeneratePaymentCancelledEmailTemplate(Guid paymentId)
        {
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #f8d7da; padding: 20px; text-align: center;'>
                    <h2 style='color: #721c24;'>Payment Cancelled</h2>
                </div>
                <div style='padding: 20px;'>
                    <p>Hello!</p>
                    <p>We inform you that your payment has been cancelled as requested.</p>
                    <p><strong>Payment ID:</strong> {paymentId}</p>
                    <p>If you would like to retry the purchase, you can access our store again.</p>
                    <p>If you have any questions, we are here to help!</p>
                    <hr style='margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Una Estúdio Criativo - Handmade Products</p>
                </div>
            </body>
            </html>";
        }

        private async Task<string> GeneratePaymentExpiredEmailTemplate(Guid paymentId)
        {
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #fff3cd; padding: 20px; text-align: center;'>
                    <h2 style='color: #856404;'>Payment Expired</h2>
                </div>
                <div style='padding: 20px;'>
                    <p>Hello!</p>
                    <p>Unfortunately, your payment has expired without confirmation.</p>
                    <p><strong>Payment ID:</strong> {paymentId}</p>
                    <p>But don't worry! You can make a new purchase attempt at any time.</p>
                    <p>Access our store again and complete your purchase!</p>
                    <hr style='margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Una Estúdio Criativo - Handmade Products</p>
                </div>
            </body>
            </html>";
        }

        private async Task<string> GeneratePaymentFailedAdminEmailTemplate(Guid paymentId, string errorMessage)
        {
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #f8d7da; padding: 20px; text-align: center;'>
                    <h2 style='color: #721c24;'>Payment Processing Failed</h2>
                </div>
                <div style='padding: 20px;'>
                    <p><strong>Administrator,</strong></p>
                    <p>A payment processing failure occurred:</p>
                    <ul style='background-color: #f8f9fa; padding: 15px; border-radius: 5px;'>
                        <li><strong>Payment ID:</strong> {paymentId}</li>
                        <li><strong>Date/Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                        <li><strong>Error:</strong> {errorMessage}</li>
                    </ul>
                    <p>Please check system logs for more details.</p>
                    <hr style='margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Automatic System - Una Estúdio Criativo</p>
                </div>
            </body>
            </html>";
        }

        private async Task<string> GenerateWebhookFailedAdminEmailTemplate(Guid paymentId, string webhookEvent, string errorMessage, int attemptCount)
        {
            return $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background-color: #fff3cd; padding: 20px; text-align: center;'>
                    <h2 style='color: #856404;'>Webhook Failed - Attempt {attemptCount}</h2>
                </div>
                <div style='padding: 20px;'>
                    <p><strong>Administrator,</strong></p>
                    <p>A webhook failed processing:</p>
                    <ul style='background-color: #f8f9fa; padding: 15px; border-radius: 5px;'>
                        <li><strong>Payment ID:</strong> {paymentId}</li>
                        <li><strong>Event:</strong> {webhookEvent}</li>
                        <li><strong>Attempt:</strong> {attemptCount}</li>
                        <li><strong>Date/Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                        <li><strong>Error:</strong> {errorMessage}</li>
                    </ul>
                    <p>{(attemptCount < 5 ? "The system will attempt to reprocess automatically." : "⚠️ Maximum attempts exceeded. Manual intervention may be required.")}</p>
                    <hr style='margin: 20px 0;'>
                    <p style='color: #666; font-size: 12px;'>Automatic System - Una Estúdio Criativo</p>
                </div>
            </body>
            </html>";
        }

        #endregion
    }

    // Configuration class for notifications
    public class NotificationOptions
    {
        public string AdminEmail { get; set; } = string.Empty;
        public bool EnableCustomerNotifications { get; set; } = true;
        public bool EnableAdminNotifications { get; set; } = true;
        public string CompanyName { get; set; } = "Una Estúdio Criativo";
        public string SupportEmail { get; set; } = string.Empty;
    }
}