using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Librarian")]
public class CatalogController : ControllerBase
{
    private readonly AppDbContext _context;

    public CatalogController(AppDbContext context)
    {
        _context = context;
    }


    // [HttpGet("api/Warehouses")]
    // public async Task<IActionResult> GetWarehouses()
    // {
    //     var items = await _context.Warehouses
    //         .OrderBy(w => w.WarehouseId)
    //         .Select(w => new { w.WarehouseId, w.Name, w.Code, w.Description, w.AllowBorrow,
    //             CopyCount = _context.BookCopies.Count(c => c.WarehouseId == w.WarehouseId) })
    //         .ToListAsync();
    //     return Ok(items);
    // }

    
}

// DTOs
public class CreateCatalogDto
{
    public string Name { get; set; } = "";
    public string? Description { get; set; } 
    public string? Bio { get; set; }         
    public string? ImageUrl { get; set; }    
}