using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Services;

namespace Backend.Controllers;
[ApiController]
[Route("api/seed")]
public class SeedController : ControllerBase
{
    private readonly BookImportService _service;

    public SeedController(BookImportService service)
    {
        _service = service;
    }

    [HttpPost("books")]
    public async Task<IActionResult> SeedBooks(string query = "programming")
    {
        await _service.ImportBooks(query);
        return Ok("Books imported");
    }
}