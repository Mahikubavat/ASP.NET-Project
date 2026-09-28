using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BlogPlatform.Services
{
    public interface IFileStorageService
    {
        Task<string?> SaveFileAsync(IFormFile file, string subFolder);
        void DeleteFileIfExists(string relativePath);
    }

    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;
        public FileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string?> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            var uploadsRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", subFolder);
            if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsRoot, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{subFolder}/{fileName}";
        }

        public void DeleteFileIfExists(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var p = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var full = Path.Combine(_env.WebRootPath ?? "wwwroot", p);
            if (File.Exists(full)) File.Delete(full);
        }
    }
}
