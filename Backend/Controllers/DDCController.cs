using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DDCController : ControllerBase
{
    private readonly AppDbContext _context;

    public DDCController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet("tree")]
    public async Task<IActionResult> GetDDCTree()
    {
        var ddcList = await _context.DDCs.ToListAsync();

        var result = BuildTree(ddcList, null);

        return Ok(result);
    }

    private List<DDCTreeDto> BuildTree(List<DDC> list, string? parentCode)
    {
        return list
        .Where(x => x.ParentCode == parentCode)
        .Select(x => new DDCTreeDto
        {
            Code = x.Code,
            Name = x.Name,
            Children = BuildTree(list, x.Code)
        })
        .ToList();
    }

    [HttpGet]
    public async Task<IActionResult> GetDDC([FromQuery] string? search)
    {
        var query = _context.DDCs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Code.Contains(search) || d.Name.Contains(search));

        var all = await query
            .OrderBy(d => d.Code)
            .Select(d => new
            {
                d.Code,
                d.Name,
                d.ParentCode,
                BookCount = _context.Books.Count(b => b.DDCCode == d.Code && !b.IsDeleted)
            })
            .ToListAsync();

        return Ok(all);
    }

    // POST /api/DDC
    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> CreateDDC([FromBody] CreateDDCDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Mã và tên không được để trống" });

        var exists = await _context.DDCs.AnyAsync(d => d.Code == dto.Code.Trim());
        if (exists) return Conflict(new { message = $"Mã DDC '{dto.Code}' đã tồn tại" });

        // Validate ParentCode
        if (!string.IsNullOrWhiteSpace(dto.ParentCode))
        {
            var parentExists = await _context.DDCs.AnyAsync(d => d.Code == dto.ParentCode);
            if (!parentExists)
                return BadRequest(new { message = $"Mã DDC cha '{dto.ParentCode}' không tồn tại" });
        }

        var item = new DDC
        {
            Code       = dto.Code.Trim(),
            Name       = dto.Name.Trim(),
            ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim()
        };

        _context.DDCs.Add(item);
        await _context.SaveChangesAsync();
        return Ok(new { item.Code, item.Name, item.ParentCode });
    }

    // PUT /api/DDC/{code}
    [HttpPut("{code}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> UpdateDDC(string code, [FromBody] CreateDDCDto dto)
    {
        var item = await _context.DDCs.FindAsync(code);
        if (item == null) return NotFound(new { message = "Không tìm thấy mã DDC" });

        item.Name       = dto.Name.Trim();
        item.ParentCode = string.IsNullOrWhiteSpace(dto.ParentCode) ? null : dto.ParentCode.Trim();
        await _context.SaveChangesAsync();
        return Ok(new { item.Code, item.Name, item.ParentCode });
    }

    // DELETE /api/DDC/{code}
    [HttpDelete("{code}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> DeleteDDC(string code)
    {
        var item = await _context.DDCs.FindAsync(code);
        if (item == null) return NotFound(new { message = "Không tìm thấy mã DDC" });

        var hasBooks = await _context.Books.AnyAsync(b => b.DDCCode == code);
        if (hasBooks) return BadRequest(new { message = "Không thể xóa mã DDC đang có sách" });

        var hasChildren = await _context.DDCs.AnyAsync(d => d.ParentCode == code);
        if (hasChildren) return BadRequest(new { message = "Không thể xóa mã DDC đang có mã con" });

        _context.DDCs.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}