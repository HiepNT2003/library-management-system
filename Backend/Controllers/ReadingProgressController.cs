using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/ReadingProgress")]
[Authorize]
public class ReadingProgressController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReadingProgressController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/ReadingProgress/{bookId}
    [HttpGet("{bookId}")]
    public async Task<IActionResult> GetProgress(int bookId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var progress = await _context.ReadingProgresses
            .FirstOrDefaultAsync(p => p.BookId == bookId && p.UserId == userId);

        if (progress == null)
            return Ok(new
            {
                currentPage  = 1,
                percentRead  = 0,
                highlights   = "[]",
                notes        = "",
                lastReadDate = (DateTime?)null
            });

        return Ok(new
        {
            progress.ProgressId,
            progress.CurrentPage,
            progress.PercentRead,
            progress.LastReadDate,
            highlights = progress.HighLights ?? "[]",
            notes      = progress.Notes ?? ""
        });
    }

    // POST /api/ReadingProgress
    [HttpPost]
    public async Task<IActionResult> SaveProgress([FromBody] SaveProgressDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var progress = await _context.ReadingProgresses
            .FirstOrDefaultAsync(p => p.BookId == dto.BookId && p.UserId == userId);

        if (progress == null)
        {
            progress = new ReadingProgress
            {
                BookId = dto.BookId,
                UserId = userId
            };
            _context.ReadingProgresses.Add(progress);
        }

        progress.CurrentPage  = dto.CurrentPage;
        progress.PercentRead  = dto.PercentRead;
        progress.LastReadDate = DateTime.UtcNow;

        if (dto.Highlights != null) progress.HighLights = dto.Highlights;
        if (dto.Notes != null)      progress.Notes      = dto.Notes;

        await _context.SaveChangesAsync();

        // Save to UserReadingHistory
        var history = await _context.UserReadingHistories
            .FirstOrDefaultAsync(h => h.UserId == userId &&
                                      h.BookId == dto.BookId &&
                                      h.Action == "Read");

        if (history == null)
        {
            _context.UserReadingHistories.Add(new UserReadingHistory
            {
                UserId    = userId!,
                BookId    = dto.BookId,
                Action    = "Read",
                CreatedAt = DateTime.Now
            });
        }
        else
        {
            history.CreatedAt        = DateTime.Now;
            history.DurationMinutes  = dto.DurationMinutes;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã lưu tiến độ", currentPage = progress.CurrentPage });
    }
}

public class SaveProgressDto
{
    public int BookId { get; set; }
    public int CurrentPage { get; set; }
    public decimal? PercentRead { get; set; }
    public string? Highlights { get; set; }
    public string? Notes { get; set; }
    public int? DurationMinutes { get; set; }
}