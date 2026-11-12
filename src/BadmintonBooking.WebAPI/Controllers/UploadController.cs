using BadmintonBooking.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BadmintonBooking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Provider,Admin")] // Chỉ chủ sân và admin được tải ảnh lên
public class UploadController : ControllerBase
{
    private readonly IStorageService _storageService;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB mỗi ảnh

    /// <summary>
    /// Kiểm tra chữ ký (magic bytes) của tệp: không tin vào đuôi file hay Content-Type do client gửi.
    /// </summary>
    private static async Task<bool> HasImageSignatureAsync(IFormFile file)
    {
        var header = new byte[12];
        using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read < 4) return false;

        bool Starts(params byte[] sig) => read >= sig.Length && header.Take(sig.Length).SequenceEqual(sig);

        if (Starts(0xFF, 0xD8, 0xFF)) return true;                                   // JPEG
        if (Starts(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)) return true;     // PNG
        if (Starts(0x47, 0x49, 0x46, 0x38)) return true;                             // GIF
        if (read >= 12 && Starts(0x52, 0x49, 0x46, 0x46)                              // WEBP: "RIFF....WEBP"
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) return true;
        return false;
    }

    public UploadController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    /// <summary>
    /// Tải một ảnh lên Cloudflare R2 (Bucket: prn232_bucket)
    /// </summary>
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)] // Chặn request quá lớn trước khi đọc
    public async Task<IActionResult> UploadImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn tệp ảnh để tải lên." });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { message = "Dung lượng tệp không được vượt quá 10MB." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) || !await HasImageSignatureAsync(file))
        {
            return BadRequest(new { message = "Tệp không phải ảnh hợp lệ. Chỉ chấp nhận: .jpg, .jpeg, .png, .webp, .gif" });
        }

        using var stream = file.OpenReadStream();
        var url = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

        return Ok(new { url, fileName = file.FileName, size = file.Length });
    }

    /// <summary>
    /// Tải nhiều ảnh cùng lúc lên Cloudflare R2 cho thư viện ảnh cơ sở (Gallery Images)
    /// </summary>
    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)] // Tối đa ~20MB tổng cho nhiều ảnh (mỗi ảnh <= 5MB)
    public async Task<IActionResult> UploadMultipleImages(List<IFormFile>? files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn ít nhất một tệp ảnh." });
        }

        var filePayloads = new List<(Stream Stream, string FileName, string ContentType)>();
        try
        {
            foreach (var file in files)
            {
                if (file.Length > MaxFileSize) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext)) continue;
                if (!await HasImageSignatureAsync(file)) continue;

                filePayloads.Add((file.OpenReadStream(), file.FileName, file.ContentType));
            }

            if (filePayloads.Count == 0)
            {
                return BadRequest(new { message = "Không có tệp ảnh hợp lệ để tải lên." });
            }

            var urls = await _storageService.UploadMultipleFilesAsync(filePayloads);
            return Ok(new { urls, count = urls.Count });
        }
        finally
        {
            foreach (var item in filePayloads)
            {
                item.Stream.Dispose();
            }
        }
    }
}

