namespace BadmintonBooking.BLL.Services.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream stream, string fileName, string contentType);
    Task<List<string>> UploadMultipleFilesAsync(IEnumerable<(Stream Stream, string FileName, string ContentType)> files);
    Task<bool> DeleteFileAsync(string fileUrl);
}

