using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/Fines")]
[Authorize(Roles = "Admin,Librarian")]
public class FinesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public FinesController(AppDbContext context, IEmailService emailService, INotificationService notificationService)
    {
        _context      = context;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    // GET /api/Fines?status=Pending&search=&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Fines
            .Include(f => f.Transaction)
                .ThenInclude(t => t.User)
                    .ThenInclude(u => u.StudentProfile)
            .Include(f => f.Transaction)
                .ThenInclude(t => t.Copy)
                    .ThenInclude(c => c.Book)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FineStatus>(status, out var statusEnum))
            query = query.Where(f => f.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f =>
                (f.Transaction.User.FullName != null && f.Transaction.User.FullName.Contains(search)) ||
                (f.Transaction.User.StudentProfile != null && f.Transaction.User.StudentProfile.StudentCode.Contains(search)) ||
                (f.Transaction.Copy.Book.Title != null && f.Transaction.Copy.Book.Title.Contains(search)));

        var total = await query.CountAsync();

        // Statistics
        var stats = await _context.Fines
            .GroupBy(f => f.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count(), Total = g.Sum(f => f.Amount) })
            .ToListAsync();

        var items = await query
            .OrderByDescending(f => f.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.FineId,
                f.Amount,
                f.Reason,
                f.Note,
                f.Status,
                f.CreatedDate,
                f.PaidDate,
                Transaction = new
                {
                    f.Transaction.TransactionId,
                    f.Transaction.BorrowDate,
                    f.Transaction.DueDate,
                    f.Transaction.ReturnDate
                },
                User = new
                {
                    f.Transaction.User.Id,
                    f.Transaction.User.FullName,
                    f.Transaction.User.Email,
                    StudentCode = f.Transaction.User.StudentProfile != null
                        ? f.Transaction.User.StudentProfile.StudentCode : null
                },
                Book = new
                {
                    f.Transaction.Copy.Book.Title,
                    f.Transaction.Copy.Barcode
                }
            })
            .ToListAsync();

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize),
            stats
        });
    }

    // PATCH /api/Fines/{id}/pay — pay fine
    [HttpPatch("{id}/pay")]
    public async Task<IActionResult> Pay(int id, [FromBody] PayFineDto dto)
    {
        var fine = await _context.Fines
            .Include(f => f.Transaction)
                .ThenInclude(t => t.User)
            .Include(f => f.Transaction)
                .ThenInclude(t => t.Copy)
                    .ThenInclude(c => c.Book)
            .FirstOrDefaultAsync(f => f.FineId == id);

        if (fine == null)
            return NotFound(new { message = "Không tìm thấy phiếu phạt" });

        if (fine.Status == FineStatus.Paid)
            return BadRequest(new { message = "Phiếu phạt đã được thu" });

        if (fine.Status == FineStatus.Waived)
            return BadRequest(new { message = "Phiếu phạt đã được miễn" });

        var librarianId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        fine.Status      = FineStatus.Paid;
        fine.PaidDate    = DateTime.Now;
        fine.PaidByUserId = librarianId;
        fine.Note        = dto.Note ?? fine.Note;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Thu phạt thành công",
            fineId  = fine.FineId,
            amount  = fine.Amount,
            paidDate = fine.PaidDate
        });
    }

    // PATCH /api/Fines/{id}/waive — waive fine
    [HttpPatch("{id}/waive")]
    public async Task<IActionResult> Waive(int id, [FromBody] WaiveFineDto dto)
    {
        var fine = await _context.Fines
            .Include(f => f.Transaction)
                .ThenInclude(t => t.User)
            .Include(f => f.Transaction)
                .ThenInclude(t => t.Copy)
                    .ThenInclude(c => c.Book)
            .FirstOrDefaultAsync(f => f.FineId == id);

        if (fine == null)
            return NotFound(new { message = "Không tìm thấy phiếu phạt" });

        if (fine.Status == FineStatus.Paid)
            return BadRequest(new { message = "Phiếu phạt đã được thu, không thể miễn" });

        if (fine.Status == FineStatus.Waived)
            return BadRequest(new { message = "Phiếu phạt đã được miễn trước đó" });

        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(new { message = "Vui lòng nhập lý do miễn phạt" });

        fine.Status = FineStatus.Waived;
        fine.Note   = dto.Reason;

        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(
            fine.Transaction.UserId,
            "Phiếu phạt đã được miễn",
            NotificationType.FineWaived,
            $"Phiếu phạt {fine.Amount:N0}đ đã được miễn. Lý do: {dto.Reason}",
            "/my-fines"
        );
        // Send an email about waiving the penalty
        if (!string.IsNullOrWhiteSpace(fine.Transaction?.User?.Email))
        {
            _ = _emailService.SendAsync(
                fine.Transaction.User.Email,
                "Thông báo miễn phạt thư viện",
                $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px;">
                  <h2 style="color: #2e7d32;">Thông báo miễn phạt ✓</h2>
                  <p>Xin chào <strong>{fine.Transaction.User.FullName}</strong>,</p>
                  <p>Phiếu phạt của bạn đã được miễn:</p>
                  <table style="width:100%; border-collapse:collapse; margin:16px 0;">
                    <tr style="background:#f5f5f5;">
                      <td style="padding:10px; border:1px solid #ddd;">Sách</td>
                      <td style="padding:10px; border:1px solid #ddd;">
                        <strong>{fine.Transaction.Copy?.Book?.Title}</strong>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:10px; border:1px solid #ddd;">Số tiền</td>
                      <td style="padding:10px; border:1px solid #ddd;">{fine.Amount:N0} VNĐ</td>
                    </tr>
                    <tr>
                      <td style="padding:10px; border:1px solid #ddd;">Lý do miễn</td>
                      <td style="padding:10px; border:1px solid #ddd;">{dto.Reason}</td>
                    </tr>
                  </table>
                  <p style="color:#888; font-size:12px;">Thư viện ĐH Giao thông Vận tải</p>
                </div>
                """
            ).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"Email error: {t.Exception?.Message}");
            });
        }

        return Ok(new { message = "Miễn phạt thành công", fineId = fine.FineId });
    }
}

public class PayFineDto
{
    public string? Note { get; set; }
}

public class WaiveFineDto
{
    public string Reason { get; set; } = "";
}