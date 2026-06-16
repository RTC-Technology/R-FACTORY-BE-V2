using Microsoft.AspNetCore.Mvc;

namespace R_FACTORY_BE.Controllers;

[ApiController]
[Route("api/files")]
public sealed class FilesController(IWebHostEnvironment env) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gif",
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    [HttpPost("layout-map")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadLayoutMap(IFormFile file)
    {
        if (file is null || file.Length == 0) return BadRequest("File is required.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension)) return BadRequest("Only image files are allowed.");

        var folder = Path.Combine(env.ContentRootPath, "Files", "layouts");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        return Ok(new { Path = $"/files/layouts/{fileName}" });
    }
}
