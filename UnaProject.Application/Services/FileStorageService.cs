using Microsoft.AspNetCore.Http;
using UnaProject.Application.Services.Interfaces;

namespace UnaProject.Application.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _baseDirectory;

        public FileStorageService(string baseDirectory)
        {
            _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
                ? throw new ArgumentNullException(nameof(baseDirectory))
                : baseDirectory;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName, string fileName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file provided or empty file.", nameof(file));
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("The container name is required.", nameof(containerName));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("The filename is required.", nameof(fileName));

            string sanitizedFileName = Path.GetFileName(fileName.Replace(" ", "_").Replace("\\", "").Replace("/", ""));
            string folderPath = Path.Combine(_baseDirectory, containerName);
            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, sanitizedFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Returns only the relative path
            return $"{containerName}/{sanitizedFileName}";
        }

        public Task<bool> DeleteFileAsync(string containerName, string fileName)
        {
            if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(fileName))
                return Task.FromResult(false);

            string filePath = Path.Combine(_baseDirectory, containerName, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
