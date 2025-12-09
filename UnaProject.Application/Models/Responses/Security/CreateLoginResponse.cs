namespace UnaProject.Application.Models.Responses.Security
{
    public class CreateLoginResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public required string Token { get; set; }
        public required string Mensage { get; set; }
    }
}
