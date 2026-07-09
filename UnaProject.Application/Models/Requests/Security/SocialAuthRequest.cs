namespace UnaProject.Application.Models.Requests.Security
{
    /// <summary>
    /// Dados de um usuário social já VALIDADOS server-side pelo provedor.
    /// NUNCA é preenchido diretamente pelo corpo da requisição do cliente — os valores
    /// são sempre derivados da validação do accessToken (ver <c>ISocialTokenValidator</c>).
    /// </summary>
    public class SocialUserInfo
    {
        public string ProviderId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty; // "google" or "facebook"
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
    }

    /// <summary>
    /// Contrato de login social. O cliente envia SOMENTE o accessToken obtido no SDK do
    /// provedor (Google/Facebook). O provider vem da rota (/api/socialauth/google|facebook).
    /// O perfil (email/nome/id) é obtido validando o token no provedor — nunca do corpo.
    /// </summary>
    public class SocialAuthRequest
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }
}
