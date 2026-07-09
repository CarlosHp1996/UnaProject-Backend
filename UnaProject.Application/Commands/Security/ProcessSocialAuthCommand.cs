using MediatR;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security
{
    /// <summary>
    /// Processa login social. Recebe apenas o provider (da rota) e o accessToken do cliente.
    /// O perfil é obtido validando o token no provedor (server-side), nunca do corpo.
    /// </summary>
    public class ProcessSocialAuthCommand : IRequest<Result<SocialAuthResponse>>
    {
        public string Provider { get; set; }
        public string AccessToken { get; set; }

        public ProcessSocialAuthCommand(string provider, string accessToken)
        {
            Provider = provider;
            AccessToken = accessToken;
        }
    }
}
