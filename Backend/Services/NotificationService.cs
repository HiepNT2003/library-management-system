using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public interface INotificationService
{
    Task CreateAsync(string userId, string title, string type, string? message = null, string? link = null);
    Task CreateForRoleAsync(string roleName, string title, string type, string? message = null, string? link = null);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    // Tạo notification cho 1 user
    public async Task CreateAsync(string userId, string title, string type, string? message = null, string? link = null)
    {
        _context.Notifications.Add(new Notification
        {
            UserId    = userId,
            Title     = title,
            Type      = type,
            Message   = message,
            Link      = link,
            IsRead    = false,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();
    }

    // Tạo notification cho tất cả user thuộc role (VD: thông báo cho tất cả Librarian)
    public async Task CreateForRoleAsync(string roleName, string title, string type, string? message = null, string? link = null)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return;

        var userIds = await _context.UserRoles
            .Where(ur => ur.RoleId == role.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var notifications = userIds.Select(uid => new Notification
        {
            UserId    = uid,
            Title     = title,
            Type      = type,
            Message   = message,
            Link      = link,
            IsRead    = false,
            CreatedAt = DateTime.Now
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();
    }
}