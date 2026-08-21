using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers;

[ApiController]
[Route("api/Warehouses")]
public class WarehousesController : ControllerBase
{
    private readonly AppDbContext _context;

    public WarehousesController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/Warehouses
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var warehouses = await _context.Warehouses
            .OrderBy(w => w.WarehouseId)
            .Select(w => new WarehouseDto
            {
                WarehouseId      = w.WarehouseId,
                Code        = w.Code,
                Name        = w.Name,
                Location    = w.Location,
                Description = w.Description,
                CopyCount   = _context.BookCopies.Count(bc => bc.WarehouseId == w.WarehouseId)
            })
            .ToListAsync();

        return Ok(warehouses);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
            return BadRequest(new { message = "Tên và mã không được để trống" });

        var exists = await _context.Warehouses.AnyAsync(w => w.Code == dto.Code.Trim());
        if (exists) return Conflict(new { message = $"Mã kho '{dto.Code}' đã tồn tại" });

        var item = new Warehouse
        {
            Name        = dto.Name.Trim(),
            Code        = dto.Code.Trim().ToUpper(),
            Location    = dto.Location,
            Description = dto.Description,
        };
        _context.Warehouses.Add(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> UpdateWarehouse(int id, [FromBody] CreateWarehouseDto dto)
    {
        var item = await _context.Warehouses.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy kho" });

        item.Name        = dto.Name.Trim();
        item.Description = dto.Description;
        item.Location    = dto.Location;
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        var item = await _context.Warehouses.FindAsync(id);
        if (item == null) return NotFound(new { message = "Không tìm thấy kho" });

        var hasCopies = await _context.BookCopies.AnyAsync(c => c.WarehouseId == id);
        if (hasCopies) return BadRequest(new { message = "Không thể xóa kho đang có bản sao sách" });

        _context.Warehouses.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class WarehouseDto
{
    public int WarehouseId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Location { get; set; }
    public string? Description { get; set; }
    public int? CopyCount { get; set; }
}


public class CreateWarehouseDto
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Location { get; set; }
    public string? Description { get; set; }
}