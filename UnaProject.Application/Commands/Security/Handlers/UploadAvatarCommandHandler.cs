using MediatR;
using Microsoft.AspNetCore.Http;
using UnaProject.Application.Services.Interfaces;
using UnaProject.Domain.Helpers;
using System.Security.Claims;
using System.IO;

namespace UnaProject.Application.Commands.Security.Handlers
{
    public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, Result<string>>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUrlHelperService _urlHelperService;

        public UploadAvatarCommandHandler(
            IFileStorageService fileStorageService, 
            IHttpContextAccessor httpContextAccessor,
            IUrlHelperService urlHelperService)
        {
            _fileStorageService = fileStorageService;
            _httpContextAccessor = httpContextAccessor;
            _urlHelperService = urlHelperService;
        }

        public async Task<Result<string>> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
        {
            var result = new Result<string>();

            try
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    result.WithError("User not authenticated.");
                    return result;
                }

                var fileName = $"{userId}_avatar{Path.GetExtension(request.File.FileName)}";
                var relativePath = await _fileStorageService.UploadFileAsync(request.File, "avatars", fileName);
                
                result.Value = _urlHelperService.GenerateImageUrl(relativePath);
                result.HasSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.WithError($"Error uploading avatar: {ex.Message}");
                return result;
            }
        }
    }
}
