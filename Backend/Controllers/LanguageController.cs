using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LanguageController : ControllerBase
{
    private readonly AppDbContext _context;

    public LanguageController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetLanguages([FromQuery] string? search)
    {
        var query = _context.Languages.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l => l.Name.Contains(search));

        var items = await query
            .OrderBy(l => l.Name)
            .Select(l => new {
                l.LanguageId, l.Code, l.Name,
                BookCount = _context.BookLanguages.Count(bl => bl.LanguageId == l.LanguageId && !bl.Book.IsDeleted)
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> UpdateLanguage(int id, [FromBody] CreateCatalogDto dto)
    {
        var item = await _context.Languages.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy ngôn ngữ" });

        item.Name = dto.Name.Trim();
        await _context.SaveChangesAsync();
        return Ok(new { item.LanguageId, item.Name });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> DeleteLanguage(int id)
    {
        var item = await _context.Languages.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy ngôn ngữ" });

        var hasBooks = await _context.BookLanguages.AnyAsync(bl => bl.LanguageId == id);
        if (hasBooks) return BadRequest(new { message = "Không thể xóa ngôn ngữ đang có sách" });

        _context.Languages.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> CreateLanguage([FromBody] CreateLanguageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Language name is required" });

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "Language code is required" });

        var code = request.Code.Trim().ToLower();
        var name = request.Name.Trim();

        var existingLanguage = await _context.Languages
            .FirstOrDefaultAsync(l =>
                l.Code.ToLower() == code ||
                l.Name.ToLower() == name.ToLower());

        if (existingLanguage != null)
            return BadRequest(new { message = "Language already exists" });

        var language = new Language
        {
            Code = code,
            Name = name
        };

        try
        {
            _context.Languages.Add(language);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Language already exists" });
        }

        return CreatedAtAction(nameof(GetLanguages), new { id = language.LanguageId }, new
        {
            message = "Create language successfully",
            data = new
            {
                language.LanguageId,
                language.Code,
                language.Name,
            }
        });
    }
}