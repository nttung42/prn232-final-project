using BadmintonBooking.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BadmintonBooking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IStorageService _storageService;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public UploadController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    /// <summary>
    /// Tải một ảnh lên Cloudflare R2 (Bucket: prn232_bucket)
    /// </summary>
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
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
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .webp, .gif" });
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

