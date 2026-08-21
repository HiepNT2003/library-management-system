using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories([FromQuery] string? search)
    {
        var query = _context.Categories.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var items = await query
            .OrderBy(c => c.Name)
            .Select(c => new { c.CategoryId, c.Name, c.Description,
                BookCount = _context.BookCategories.Count(bc => bc.CategoryId == c.CategoryId && !bc.Book.IsDeleted) })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateCatalogDto dto)
    {
        var item = await _context.Categories.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy thể loại" });

        item.Name        = dto.Name.Trim();
        item.Description = dto.Description;
        await _context.SaveChangesAsync();
        return Ok(new { item.CategoryId, item.Name, item.Description });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var item = await _context.Categories.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy thể loại" });

        var hasBooks = await _context.BookCategories.AnyAsync(bc => bc.CategoryId == id);
        if (hasBooks) return BadRequest(new { message = "Không thể xóa thể loại đang có sách" });

        _context.Categories.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        // validate
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Tên không được để trống" });
        }

        // check duplicate name
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(a => a.Name == request.Name);

        if (existingCategory != null)
        {
            return BadRequest(new { message = "Category already exists" });
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description,
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Create category successfully",
            data = new
            {
                category.CategoryId,
                category.Name,
                category.Description,
            }
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(int id)
    {
        var item = await _context.Categories.FindAsync(id);
        if (item == null) return NotFound();
        return Ok( new {item.CategoryId, item.Name, item.Description});
    }
}