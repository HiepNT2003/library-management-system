using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/upload")]
public class UploadController : ControllerBase
{
    private readonly CloudinaryService _cloudinaryService;
    private readonly AppDbContext _context;

    public UploadController(CloudinaryService cloudinaryService, AppDbContext context)
    {
        _cloudinaryService = cloudinaryService;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return BadRequest("type is required");
        }
        if (file == null || file.Length == 0)
            return BadRequest();
        
        var folder = type switch
        {
            "book" => "books",
            "author" => "authors",
            _ => "others"
        };

        var url = await _cloudinaryService.UploadImageAsync(file, folder);

        return Ok(new { url });
    }
    [HttpPost("file")]
    public async Task<IActionResult> UploadEbook(IFormFile file)
    {
        try
        {
            var (url, size) = await _cloudinaryService.UploadEbookAsync(file);

            return Ok(new
            {
                filePath = url,
                fileSize = size
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("ebooks/{id}")]
    public async Task<IActionResult> GetEbook(int id)
    {
        var ebook = await _context.Books.FindAsync(id);

        if (ebook == null) return NotFound();

        var client = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Get, ebook.FilePath);

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var stream = await response.Content.ReadAsStreamAsync();

        return File(stream, "application/pdf", enableRangeProcessing: true);
    }

    [HttpGet("download/{bookId}")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadBook(int bookId)
    {
        var book = await _context.Books
            .Where(b => b.BookId == bookId && !b.IsDeleted)
            .Select(b => new { b.FilePath, b.Title, b.IsPublic, b.DocumentTypeId })
            .FirstOrDefaultAsync();

        if (book == null || string.IsNullOrEmpty(book.FilePath))
            return NotFound(new { message = "Không tìm thấy file" });

        if (book.IsPublic != true)
            return BadRequest(new { message = "Tài liệu này không hỗ trợ tải về" });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Vui lòng đăng nhập để tải tài liệu" });

        if (!book.FilePath.Contains("res.cloudinary.com"))
            return BadRequest(new { message = "Invalid file source" });

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(book.FilePath);
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Cannot fetch file");
        
        var bookEntity = await _context.Books.FindAsync(bookId);
        if (bookEntity != null)
        {
            bookEntity.DownloadCount++;
            await _context.SaveChangesAsync();
        }

        var stream   = await response.Content.ReadAsStreamAsync();
        var fileName = $"{book.Title}.pdf";
        return File(stream, "application/pdf", fileName);
    }
}