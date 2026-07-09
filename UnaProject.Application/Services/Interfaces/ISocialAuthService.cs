using UnaProject.Application.Models.Requests.Security;
using UnaProject.Application.Models.Responses.Security;
using UnaProject.Domain.Entities.Security;

namespace UnaProject.Application.Services.Interfaces
{
    public interface ISocialAuthService
    {
        Task<SocialAuthResponse> ProcessSocialUserAsync(SocialUserInfo socialUser);
        Task<ApplicationUser> CreateUserFromSocialAsync(SocialUserInfo socialUser);
        Task<ApplicationUser> LinkSocialAccountAsync(ApplicationUser user, SocialUserInfo socialUser);
        Task UpdateSocialLoginAsync(ApplicationUser user, SocialUserInfo socialUser);
    }
}