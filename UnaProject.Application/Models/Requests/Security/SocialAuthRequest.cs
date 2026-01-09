namespace UnaProject.Application.Models.Requests.Security
{
    public class SocialUserInfo
    {
        public string ProviderId { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty; // "google" or "facebook"
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
    }

    public class SocialAuthRequest
    {
        public SocialUserInfo SocialUser { get; set; } = new();
        public string? ReturnUrl { get; set; }
    }
}
