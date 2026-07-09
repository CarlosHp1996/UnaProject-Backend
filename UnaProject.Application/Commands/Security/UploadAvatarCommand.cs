using MediatR;
using Microsoft.AspNetCore.Http;
using UnaProject.Domain.Helpers;

namespace UnaProject.Application.Commands.Security
{
    public class UploadAvatarCommand : IRequest<Result<string>>
    {
        public IFormFile File { get; set; }

        public UploadAvatarCommand(IFormFile file)
        {
            File = file;
        }
    }
}
