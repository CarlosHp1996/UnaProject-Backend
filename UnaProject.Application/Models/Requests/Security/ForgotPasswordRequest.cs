namespace UnaProject.Application.Models.Requests.Security
{
    /// <summary>
    /// Corpo de POST /api/auth/forgot-password. O e-mail vem no body (não mais na query string),
    /// evitando que o endereço apareça em logs de acesso/histórico de URL.
    /// </summary>
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
