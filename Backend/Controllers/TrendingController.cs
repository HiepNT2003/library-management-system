using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrendingController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrendingController(AppDbContext context)
    {
        _context = context;
    }

    // [HttpGet]
    // public async Task<IActionResult> GetTrending()
    // {
    //     var books = await _context.Transactions
    //         .GroupBy(b => b.BookId)
    //         .OrderByDescending(g => g.Count())
    //         .Take(10)
    //         .Select(g => g.First().Book)
    //         .ToListAsync();

    //     return Ok(books);
    // }
}