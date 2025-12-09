using Microsoft.Extensions.Configuration;
using Resend;
using System.Net.Mail;
using System.Text.RegularExpressions;
using UnaProject.Application.Services.Interfaces;

namespace UnaProject.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly ResendClient _resendClient;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public EmailService(IConfiguration configuration)
        {
            var apiKey = Environment.GetEnvironmentVariable("API_KEY_RESEND_UNA") ??
                         configuration["Resend:API_KEY_RESEND_UNA"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("The environment variable 'API_KEY_RESEND_UNA' is not configured..");

            _resendClient = (ResendClient)ResendClient.Create(apiKey);

            _senderEmail = Environment.GetEnvironmentVariable("SENDER_EMAIL") ??
                           configuration["Resend:SENDER_EMAIL"];

            _senderName = Environment.GetEnvironmentVariable("SENDER_NAME_UNA") ??
                          configuration["Resend:SENDER_NAME_UNA"];
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailMessage = new EmailMessage
            {
                From = $"{_senderName} <{_senderEmail}>",
                To = new[] { toEmail },
                Subject = subject,
                HtmlBody = body
            };

            var response = await _resendClient.EmailSendAsync(emailMessage);

            if (response.Exception != null)
            {
                throw new Exception($"Error sending email with Resend: {response.Exception.Message}");
            }
        }

        public async Task SendEmailConfirmationAsync(string email)
        {
            string subject = "Conta criada com sucesso - AJUSTAR##########################################";
            string body = @"
            <html>
              <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
                <table align='center' width='100%' cellpadding='0' cellspacing='0' style='max-width: 600px; background-color: #ffffff; margin: 20px auto; border-radius: 10px; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                  <tr>
                    <td style='background-color: #111827; padding: 30px 20px; border-top-left-radius: 10px; border-top-right-radius: 10px;'>
                      <h1 style='margin: 0; color: #ffffff; text-align: center; font-size: 24px;'>Conta criada com sucesso! 🎉</h1>
                    </td>
                  </tr>
                  <tr>
                    <td style='padding: 30px 20px; color: #333;'>
                      <p style='font-size: 16px;'>Olá,</p>
                      <p style='font-size: 16px; line-height: 1.6;'>
                        Sua conta foi criada com sucesso em nossa loja de suplementos <strong>Power Rock Supplements</strong>. Agora você pode acessar sua área de cliente, acompanhar seus pedidos, receber ofertas exclusivas e muito mais.
                      </p>
                      <p style='font-size: 16px; line-height: 1.6;'>
                        Para começar a explorar, clique no botão abaixo:
                      </p>
                      <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://una.com.br.AJUSTAR' style='background-color: #10b981; color: white; padding: 14px 30px; text-decoration: none; font-size: 16px; border-radius: 6px;'>Acessar minha conta</a>
                      </div>
                      <p style='font-size: 14px; color: #666;'>Se você não criou essa conta, pode ignorar este email com segurança.</p>
                    </td>
                  </tr>
                  <tr>
                    <td style='background-color: #f3f4f6; padding: 20px; border-bottom-left-radius: 10px; border-bottom-right-radius: 10px; text-align: center;'>
                      <p style='margin: 0; font-size: 14px; color: #777;'>Dúvidas? <a href='https://chat.whatsapp.com/AJUSTAR' style='color: #3b82f6; text-decoration: none;'>Fale conosco</a></p>
                      <p style='margin-top: 10px; font-size: 14px; color: #999;'>Power Rock Supplements © 2025</p>
                    </td>
                  </tr>
                </table>
              </body>
            </html>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendEmailConfirmationOrderAsync(string email)
        {
            string subject = "Pedido realizado com sucesso - AJUSTAR##########################################";
            string body = @"
            <html>
              <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
                <table align='center' width='100%' cellpadding='0' cellspacing='0' style='max-width: 600px; background-color: #ffffff; margin: 20px auto; border-radius: 10px; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                  <tr>
                    <td style='background-color: #111827; padding: 30px 20px; border-top-left-radius: 10px; border-top-right-radius: 10px;'>
                      <h1 style='margin: 0; color: #ffffff; text-align: center; font-size: 24px;'>Pedido realizado com sucesso! 🎉</h1>
                    </td>
                  </tr>
                  <tr>
                    <td style='padding: 30px 20px; color: #333;'>
                      <p style='font-size: 16px;'>Olá,</p>
                      <p style='font-size: 16px; line-height: 1.6;'>
                        Seu pedido foi realizado com sucesso em nossa loja de suplementos <strong>Power Rock Supplements</strong>. Agora você pode acompanhar o status do seu pedido e receber atualizações por email.
                      </p>
                      <p style='font-size: 16px; line-height: 1.6;'>
                        Para ver os detalhes do seu pedido, clique no botão abaixo:
                      </p>
                      <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://procksuplementos.com.br/dashboard.html' style='background-color: #10b981; color: white; padding: 14px 30px; text-decoration: none; font-size: 16px; border-radius: 6px;'>Ver meu pedido</a>
                      </div>
                      <p style='font-size: 14px; color: #666;'>Se você não realizou esse pedido, pode ignorar este email com segurança.</p>
                    </td            
                  </tr>
                  <tr>
                    <td style='background-color: #f3f4f6; padding: 20px; border-bottom-left-radius: 10px; border-bottom-right-radius: 10px; text-align: center;'>
                      <p style='margin: 0; font-size: 14px; color: #777;'>Dúvidas? <a href='https://chat.whatsapp.com/BaPWRXKLHrHA9tys9Ku1Hd' style='color: #3b82f6; text-decoration: none;'>Fale conosco</a></p>
                      <p style='margin-top: 10px; font-size: 14px; color: #999;'>Power Rock Supplements © 2025</p>
                    </td>
                  </tr>
                </table>
              </body>
            </html>";
            await SendEmailAsync(email, subject, body);
        }

        public async Task SendEmailForgoutPasswordAsync(string email)
        {
            string encodedEmail = Uri.EscapeDataString(email);

            string subject = "Recuperação de senha - AJUSTAR##########################################";
            string body = $@"
            <html>
              <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
                <table align='center' width='100%' cellpadding='0' cellspacing='0' style='max-width: 600px; background-color: #ffffff; margin: 20px auto; border-radius: 10px; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                  <tr>
                    <td style='background-color: #111827; padding: 30px 20px; border-top-left-radius: 10px; border-top-right-radius: 10px;'>
                      <h1 style='margin: 0; color: #ffffff; text-align: center; font-size: 24px;'>Recuperação de senha</h1>
                    </td>
                  </tr>
                  <tr>
                    <td style='padding: 30px 20px; color: #333;'>
                      <p style='font-size: 16px;'>Olá,</p>
                      <p style='font-size: 16px; line-height: 1.6;'>
                        Recebemos uma solicitação para redefinir sua senha na loja de suplementos <strong>Power Rock Supplements</strong>. Se você não solicitou essa alteração, pode ignorar este email.
                      </p>
                      <p style='font-size: 16px; line-height: 1.6;'>
                        Para redefinir sua senha, clique no botão abaixo:
                      </p>
                      <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://procksuplementos.com.br/forgout-password.html?email={encodedEmail}' style='background-color: #10b981; color: white; padding: 14px 30px; text-decoration: none; font-size: 16px; border-radius: 6px;'>Redefinir minha senha</a>
                      </div>
                      <p style='font-size: 14px; color: #666;'>Se você não solicitou essa alteração, pode ignorar este email com segurança.</p>
                      <p style='font-size: 12px; color: #999;'>Este link é válido apenas para o email: {email}</p>
                    </td>
                  </tr>
                  <tr>
                    <td style='background-color: #f3f4f6; padding: 20px; border-bottom-left-radius: 10px; border-bottom-right-radius: 10px; text-align: center;'>
                      <p style='margin: 0; font-size: 14px; color: #777;'>Dúvidas? <a href='https://chat.whatsapp.com/BaPWRXKLHrHA9tys9Ku1Hd' style='color: #3b82f6; text-decoration: none;'>Fale conosco</a></p>
                      <p style='margin-top: 10px; font-size: 14px; color: #999;'>Power Rock Supplements © 2025</p>
                    </td>
                  </tr>
                </table>
              </body>
            </html>";
            await SendEmailAsync(email, subject, body);
        }

        public async Task SendEmailConfirmationTrackingAsync(string email)
        {
            string subject = "Rastreamento do pedido - AJUSTAR##########################################";
            string body = @"
            <html>
              <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
                <table align='center' width='100%' cellpadding='0' cellspacing='0' style='max-width: 600px; background-color: #ffffff; margin: 20px auto; border-radius: 10px; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                  <tr>
                    <td style='background-color: #111827; padding: 30px 20px; border-top-left-radius: 10px; border-top-right-radius: 10px;'>
                      <h1 style='margin: 0; color: #ffffff; text-align: center; font-size: 24px;'>Rastreamento do pedido</h1>
                    </td>
                  </tr>
                  <tr>
                    <td style='padding: 30px 20px; color: #333;'>
                      <p style='font-size: 16px;'>Olá,</p>
                      <p style='font-size: 16px; line-height: 1.6;'>
                        Seu pedido está a caminho! Acompanhe o status do seu pedido e fique por dentro de todas as atualizações.
                      </p>
                      <p style='font-size: 16px; line-height: 1.6;'>
                        Para rastrear seu pedido, clique no botão abaixo:
                      </p>
                      <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://procksuplementos.com.br/tracking.html' style='background-color: #10b981; color: white; padding: 14px 30px; text-decoration: none; font-size: 16px; border-radius: 6px;'>Rastrear meu pedido</a>
                      </div>
                      <p style='font-size: 14px; color: #666;'>Se você não realizou esse pedido, pode ignorar este email com segurança.</p>
                    </td>
                  </tr>
                  <tr>
                    <td style='background-color: #f3f4f6; padding: 20px; border-bottom-left-radius: 10px; border-bottom-right-radius: 10px; text-align: center;'>
                      <p style='margin: 0; font-size: 14px; color: #777;'>Dúvidas? <a href='https://chat.whatsapp.com/BaPWRXKLHrHA9tys9Ku1Hd' style='color: #3b82f6; text-decoration: none;'>Fale conosco</a></p>
                      <p style='margin-top: 10px; font-size: 14px; color: #999;'>Power Rock Supplements © 2025</p>
                    </td>
                    </tr>
                </table>
                </body>
            </html>";
            await SendEmailAsync(email, subject, body);
        }

        public async Task<bool> IsValidEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var mailAddress = new MailAddress(email);

                if (!email.Contains("@") || !email.Split('@')[1].Contains("."))
                    return false;

                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}
