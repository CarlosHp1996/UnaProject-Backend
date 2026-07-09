using UnaProject.Application.Models.Requests.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Services.Interfaces
{
    /// <summary>
    /// Valida SERVER-SIDE um accessToken de provedor social (Google/Facebook) chamando o
    /// próprio provedor. Retorna o perfil (SocialUserInfo) derivado da resposta validada,
    /// nunca de dados enviados pelo cliente. Em caso de token inválido/erro de rede,
    /// retorna um ResultValue de erro (nunca lança exceção crua).
    /// </summary>
    public interface ISocialTokenValidator
    {
        Task<ResultValue<SocialUserInfo>> ValidateAsync(string provider, string accessToken, CancellationToken cancellationToken);
    }
}
