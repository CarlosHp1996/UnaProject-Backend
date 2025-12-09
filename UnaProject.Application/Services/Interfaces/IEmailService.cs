namespace UnaProject.Application.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendEmailConfirmationAsync(string email);
        Task SendEmailConfirmationOrderAsync(string email);
        Task SendEmailForgoutPasswordAsync(string email);
        Task SendEmailConfirmationTrackingAsync(string email);
        Task<bool> IsValidEmailAsync(string email);
    }
}
