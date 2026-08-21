using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PopularController : ControllerBase
{
    private readonly AppDbContext _context;

    public PopularController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPopular()
    {
        var items = await _context.Books
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.BookCopies.SelectMany(c => c.Transactions).Count())
            .Take(12)
            .Select(b => new
            {
                b.BookId, b.Title, b.ImageUrl, b.PublishedYear, b.DocumentTypeId,
                Authors         = b.BookAuthors.Select(ba => ba.Author.Name),
                AvailableCopies = b.BookCopies.Count(c => c.Status == BookCopyStatus.Available),
                BorrowCount     = b.BookCopies.SelectMany(c => c.Transactions).Count()
            })
            .ToListAsync();

        return Ok(items);
    }
}