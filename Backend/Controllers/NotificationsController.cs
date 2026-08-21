using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/Notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/Notifications?page=1&pageSize=20&unreadOnly=false
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();
        
        if(!string.IsNullOrWhiteSpace(type))
            query = query.Where(n => n.Type == type);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync();
        var unreadCount = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new
            {
                n.NotificationId,
                n.Title,
                n.Message,
                n.Type,
                n.Link,
                n.IsRead,
                n.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            items, total, page, pageSize,
            totalPages  = (int)Math.Ceiling((double)total / pageSize),
            unreadCount
        });
    }

    // GET /api/Notifications/unread-count
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var count  = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
        return Ok(new { count });
    }

    // PATCH /api/Notifications/{id}/read
    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notif  = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

        if (notif == null) return NotFound();

        notif.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã đánh dấu đã đọc" });
    }

    // PATCH /api/Notifications/read-all
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        return Ok(new { message = "Đã đánh dấu tất cả đã đọc" });
    }

    // DELETE /api/Notifications/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var notif  = await _context.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == userId);

        if (notif == null) return NotFound();

        _context.Notifications.Remove(notif);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/Notifications/clear
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _context.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ExecuteDeleteAsync();
        return Ok(new { message = "Đã xóa thông báo đã đọc" });
    }
}