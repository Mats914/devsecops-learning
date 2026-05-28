using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace DevSecOpsApi.Services;

public interface IImageService
{
    Task<string?> SaveImageAsync(IFormFile file);
    void          DeleteImage(string? path);
}

public class ImageService(IConfiguration config, ILogger<ImageService> logger) : IImageService
{
    private readonly long     _maxSize     = config.GetValue<long>("Upload:MaxFileSizeBytes", 5_242_880);
    private readonly string[] _allowedExts = config.GetSection("Upload:AllowedExtensions")
                                                    .Get<string[]>() ?? [".jpg", ".png"];
    private readonly string   _storagePath = config["Upload:StoragePath"] ?? "wwwroot/uploads";

    public async Task<string?> SaveImageAsync(IFormFile file)
    {
        if (file.Length > _maxSize) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExts.Contains(ext)) return null;

        // Validate it's actually an image (not just extension-spoofed)
        try
        {
            using var stream = file.OpenReadStream();
            using var img    = await Image.LoadAsync(stream);

            // Resize if too large (max 1200px wide)
            if (img.Width > 1200)
                img.Mutate(x => x.Resize(1200, 0));

            Directory.CreateDirectory(_storagePath);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(_storagePath, fileName);
            await img.SaveAsync(fullPath);
            return fileName;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid image upload rejected");
            return null;
        }
    }

    public void DeleteImage(string? path)
    {
        if (path is null) return;
        var fullPath = Path.Combine(_storagePath, path);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
