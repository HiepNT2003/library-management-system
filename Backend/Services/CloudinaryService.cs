using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IOptions<CloudinarySettings> config)
    {
        var settings = config.Value;

        var account = new Account(
            settings.CloudName,
            settings.ApiKey,
            settings.ApiSecret
        );

        _cloudinary = new Cloudinary(account);
    }

    // ===== IMAGE =====
    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        return result.SecureUrl.ToString();
    }

    // ===== EBOOK FILE =====
    public async Task<(string url, decimal size)> UploadEbookAsync(IFormFile file, string folder = "ebooks")
    {
        if (file == null || file.Length == 0)
            throw new Exception("File is empty");

        var allowedExtensions = new[] { ".pdf", ".epub" };
        var ext = Path.GetExtension(file.FileName).ToLower();

        if (!allowedExtensions.Contains(ext))
            throw new Exception("Only PDF or EPUB files are allowed");

        if (file.Length > 50 * 1024 * 1024)
            throw new Exception("File size exceeds 50MB");

        await using var stream = file.OpenReadStream();

        var publicId = Guid.NewGuid().ToString();

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = publicId,
            Type = "upload",
            Overwrite = false,
            // ResourceType = ResourceType.Auto,
            AccessMode = "public"
            // 👉 nếu muốn private thì thêm:
            // Type = "authenticated"
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error != null)
            throw new Exception(result.Error.Message);

        return (
            result.SecureUrl.ToString(), // bạn đang lưu full URL
            file.Length
        );
    }

    // ===== GENERATE DOWNLOAD URL =====
    public string GenerateDownloadUrl(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl))
            throw new Exception("File URL is empty");

        // 🔥 thêm fl_attachment vào URL
        return fileUrl.Replace("/upload/", "/upload/fl_attachment/");
    }
}