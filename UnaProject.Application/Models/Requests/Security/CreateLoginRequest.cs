namespace UnaProject.Application.Models.Requests.Security
{
    public class CreateLoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
