using MediatR;
using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security
{
    public class ProcessSocialAuthCommand : IRequest<Result<SocialAuthResponse>>
    {
        public SocialUserInfo SocialUser { get; set; }

        public ProcessSocialAuthCommand(SocialUserInfo socialUser)
        {
            SocialUser = socialUser;
        }
    }
}