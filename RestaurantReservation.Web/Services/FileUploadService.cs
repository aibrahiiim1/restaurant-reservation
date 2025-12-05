using RestaurantReservation.Web.Services.Interfaces;

namespace RestaurantReservation.Web.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public FileUploadService(IWebHostEnvironment environment, ILogger<FileUploadService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException($"File size exceeds the maximum allowed size of {MaxFileSize / 1024 / 1024}MB");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File type {extension} is not allowed");
        }

        var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", folder);
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        try
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            
            _logger.LogInformation("File uploaded successfully: {FilePath}", filePath);
            return $"/uploads/{folder}/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            throw;
        }
    }

    public async Task<List<string>> UploadFilesAsync(IList<IFormFile> files, string folder)
    {
        var urls = new List<string>();
        foreach (var file in files)
        {
            var url = await UploadFileAsync(file, folder);
            urls.Add(url);
        }
        return urls;
    }

    public Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return Task.FromResult(false);
            }

            var relativePath = fileUrl.TrimStart('/');
            var filePath = Path.Combine(_environment.WebRootPath, relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return Task.FromResult(false);
        }
    }

    public string GetFullUrl(string relativePath)
    {
        return relativePath;
    }
}
