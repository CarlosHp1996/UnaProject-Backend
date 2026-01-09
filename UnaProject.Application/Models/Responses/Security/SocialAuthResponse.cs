using UnaProject.Application.Models.Dtos;

namespace UnaProject.Application.Models.Responses.Security
{
    public class SocialAuthResponse
    {
        public string JwtToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public UserDto User { get; set; } = new();
        public bool IsNewUser { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
