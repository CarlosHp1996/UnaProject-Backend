using UnaProject.Application.Models.Dtos;

namespace UnaProject.Application.Models.Responses.Security
{
    public class CreateUserResponse
    {
        public UserDto User { get; set; }
        public required string Message { get; set; }
    }
}
