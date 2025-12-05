namespace RestaurantReservation.Web.Services.Interfaces;

public interface IFileUploadService
{
    Task<string> UploadFileAsync(IFormFile file, string folder);
    Task<List<string>> UploadFilesAsync(IList<IFormFile> files, string folder);
    Task<bool> DeleteFileAsync(string fileUrl);
    string GetFullUrl(string relativePath);
}
