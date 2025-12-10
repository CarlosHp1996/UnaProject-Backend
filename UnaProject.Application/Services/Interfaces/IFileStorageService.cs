using Microsoft.AspNetCore.Http;

namespace UnaProject.Application.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string containerName, string fileName);
        Task<bool> DeleteFileAsync(string containerName, string fileName);
    }
}
