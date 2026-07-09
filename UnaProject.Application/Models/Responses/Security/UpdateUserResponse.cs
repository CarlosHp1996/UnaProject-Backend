using UnaProject.Application.Models.Dtos;

namespace UnaProject.Application.Models.Responses.Security
{
    public class UpdateUserResponse
    {
        public UserDto User { get; set; } = null!;
        public string Message { get; set; } = string.Empty;
    }
}
