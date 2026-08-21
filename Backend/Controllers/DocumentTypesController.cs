using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentTypesController : ControllerBase
{
    private readonly AppDbContext _context;

    public DocumentTypesController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetDocumentTypes()
        {
            var query = _context.DocumentTypes.AsQueryable();

            var documentTypes = await query
                .OrderBy(dt => dt.DocumentTypeId)
                .Select(dt => new
                {
                    dt.DocumentTypeId,
                    dt.Name,
                    TotalBooks = _context.Books.Count(b => b.DocumentTypeId == dt.DocumentTypeId)
                })
                .ToListAsync();

            return Ok(documentTypes);
        }
}