using UnaProject.Application.Models.Dtos;

namespace UnaProject.Application.Models.Responses.Security
{
    public class UpdateUserResponse
    {
        public UserDto User { get; set; }
        public string Message { get; set; }
    }
}
