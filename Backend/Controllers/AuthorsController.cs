using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthorsController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAuthors([FromQuery] string? search, int? limit)
    {
        var query = _context.Authors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search));

         var querySelect = query
        .OrderBy(a => a.Name)
        .Select(a => new {
            a.AuthorId,
            a.Name,
            a.Bio,
            a.ImageUrl,
            BookCount = _context.BookAuthors.Count(ba => ba.AuthorId == a.AuthorId && !ba.Book.IsDeleted)
        });
                
        if(limit.HasValue && limit > 0)
        {
            querySelect = querySelect.Take(limit.Value);
        }

        var items = await querySelect.ToListAsync();
        return Ok(items);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> UpdateAuthor(int id, [FromBody] CreateCatalogDto dto)
    {
        var item = await _context.Authors.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy tác giả" });

        item.Name     = dto.Name.Trim();
        item.Bio      = dto.Bio;
        item.ImageUrl = dto.ImageUrl;
        await _context.SaveChangesAsync();
        return Ok(new { item.AuthorId, item.Name, item.Bio, item.ImageUrl });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> DeleteAuthor(int id)
    {
        var item = await _context.Authors.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy tác giả" });

        var hasBooks = await _context.BookAuthors.AnyAsync(ba => ba.AuthorId == id);
        if (hasBooks) return BadRequest(new { message = "Không thể xóa tác giả đang có sách" });

        _context.Authors.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> CreateAuthor([FromBody] CreateAuthorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Tên không được để trống" });
        }

        var existingAuthor = await _context.Authors
            .FirstOrDefaultAsync(a => a.Name == request.Name);

        if (existingAuthor != null)
        {
            return BadRequest(new { message = "Author already exists" });
        }

        var author = new Author
        {
            Name = request.Name.Trim(),
            Bio = request.Bio,
            ImageUrl = request.ImageUrl
        };

        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Create author successfully",
            data = new
            {
                author.AuthorId,
                author.Name,
                author.Bio,
                author.ImageUrl
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAuthor(int id)
    {
        var item = await _context.Authors.FindAsync(id);
        if(item == null) return NotFound();
        return Ok(new { item.AuthorId, item.Name, item.Bio, item.ImageUrl});
    }
}