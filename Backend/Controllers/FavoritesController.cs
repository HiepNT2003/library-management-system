using Backend.DTOs.Books;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs.Books;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FavoritesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FavoritesController(AppDbContext context)
    {
        _context = context;
    }

    // POST /api/Favorites/{id}/favorite
    [HttpPost("{id}/favorite")]
    [Authorize]
    public async Task<IActionResult> AddFavorite(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var exists = await _context.UserFavoriteBooks
            .AnyAsync(f => f.UserId == userId && f.BookId == id);

        if (exists)
            return BadRequest(new { message = "Đã có trong danh sách yêu thích" });

        _context.UserFavoriteBooks.Add(new UserFavoriteBook
        {
            UserId      = userId!,
            BookId      = id,
            CreatedDate = DateTime.Now
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã thêm vào yêu thích" });
    }

    // DELETE /api/Favorites/{id}/favorite
    [HttpDelete("{id}/favorite")]
    [Authorize]
    public async Task<IActionResult> RemoveFavorite(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var fav = await _context.UserFavoriteBooks
            .FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == id);

        if (fav == null)
            return NotFound();

        _context.UserFavoriteBooks.Remove(fav);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã bỏ yêu thích" });
    }

    // GET /api/Favorites/{id}/favorite
    [HttpGet("{id}/favorite")]
    [Authorize]
    public async Task<IActionResult> CheckFavorite(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isFav  = await _context.UserFavoriteBooks
            .AnyAsync(f => f.UserId == userId && f.BookId == id);
        return Ok(new { isFavorite = isFav });
    }
}