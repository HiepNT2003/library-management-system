using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/Transactions")]
public class TransactionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public TransactionsController(AppDbContext context, IEmailService emailService, INotificationService notificationService)
    {
        _context = context;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    // GET /api/Transactions?status=Borrowed&search=&page=1&pageSize=20
    [HttpGet]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] bool? overdueOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Transactions
            .Include(t => t.User)
                .ThenInclude(u => u.StudentProfile)
            .Include(t => t.Copy)
                .ThenInclude(c => c.Book)
            .AsQueryable();

        // Lazy update overdue
        var overdueIds = await _context.Transactions
            .Where(t => t.Status == TransactionStatus.Borrowed && t.DueDate < DateTime.Now)
            .Select(t => t.TransactionId)
            .ToListAsync();

        if (overdueIds.Any())
        {
            await _context.Transactions
                .Where(t => overdueIds.Contains(t.TransactionId))
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TransactionStatus.Overdue));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TransactionStatus>(status, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);

        if (overdueOnly == true)
            query = query.Where(t => t.Status == TransactionStatus.Overdue);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                (t.User.FullName != null && t.User.FullName.Contains(search)) ||
                (t.User.StudentProfile != null && t.User.StudentProfile.StudentCode.Contains(search)) ||
                (t.Copy.Barcode != null && t.Copy.Barcode.Contains(search)) ||
                (t.Copy.Book.Title != null && t.Copy.Book.Title.Contains(search)));

        var total = await query.CountAsync();

        var stats = await _context.Transactions
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        var itemsRaw = await query
            .OrderByDescending(t => t.BorrowDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = itemsRaw.Select(t => new
        {
            t.TransactionId,
            t.BorrowDate,
            t.DueDate,
            t.ReturnDate,
            t.Status,
            t.Notes,
            IsOverdue = t.DueDate < DateTime.Now && t.ReturnDate == null,
            OverdueDays = t.DueDate < DateTime.Now && t.ReturnDate == null
                ? (int)(DateTime.Now - t.DueDate).TotalDays
                : 0,
            User = new
            {
                t.User.Id,
                t.User.FullName,
                StudentCode = t.User.StudentProfile?.StudentCode
            },
            Copy = new
            {
                t.Copy.CopyId,
                t.Copy.Barcode,
                BookTitle = t.Copy.Book?.Title,
                BookId = t.Copy.Book?.BookId
            }
        }).ToList();

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

    // POST /api/Transactions/batch — cho mượn nhiều cuốn cùng lúc
    [HttpPost("batch")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchTransactionDto dto)
    {
        // Validate input
        if (dto.Copies == null || dto.Copies.Count == 0)
            return BadRequest(new { message = "Vui lòng chọn ít nhất 1 cuốn sách" });

        // Find user
        var user = await _context.Users
            .Include(u => u.StudentProfile)
            .Include(u => u.StaffProfile)
            .FirstOrDefaultAsync(u => u.Id == dto.UserId);

        if (user == null)
            return NotFound(new { message = "Không tìm thấy bạn đọc" });

        if (user.Status == UserStatus.Blocked)
            return BadRequest(new { message = "Tài khoản bạn đọc đã bị khóa" });

        if (user.ExpiredDate.HasValue && user.ExpiredDate < DateTime.Now)
            return BadRequest(new { message = "Thẻ thư viện của bạn đọc đã hết hạn" });

        var borrowUserRoles = await _context.UserRoles
            .Where(ur => ur.UserId == dto.UserId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var borrowFineDeadline = await _context.BorrowPolicies
            .Where(p => borrowUserRoles.Contains(p.AspNetRoleId))
            .Select(p => p.FinePaymentDeadlineDays)
            .FirstOrDefaultAsync();

        if (borrowFineDeadline == 0) borrowFineDeadline = 7;

        var borrowerHasOverdueFine = await _context.Fines
            .AnyAsync(f => f.Transaction.UserId == dto.UserId &&
                        f.Status == FineStatus.Pending &&
                        f.CreatedDate < DateTime.Today.AddDays(-borrowFineDeadline));

        if (borrowerHasOverdueFine)
            return BadRequest(new
            {
                message = $"Bạn đọc có phiếu phạt quá {borrowFineDeadline} ngày chưa thanh toán."
            });

        // Check if there's a RequestId
        BorrowRequest? request = null;
        if (dto.RequestId.HasValue)
        {
            request = await _context.BorrowRequests.FindAsync(dto.RequestId);
            if (request == null)
                return NotFound(new { message = "Không tìm thấy yêu cầu mượn" });
            if (request.Status != RequestStatus.Approved)
                return BadRequest(new { message = "Yêu cầu mượn chưa được duyệt" });
            if (request.UserId != dto.UserId)
                return BadRequest(new { message = "Yêu cầu mượn không thuộc về bạn đọc này" });
        }

        // Get all copies by barcode
        var barcodes = dto.Copies.Select(c => c.Barcode).Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
        var copyIds  = dto.Copies.Where(c => c.CopyId.HasValue).Select(c => c.CopyId!.Value).ToList();

        var copies = await _context.BookCopies
            .Include(c => c.Book)
            .Where(c => (c.Barcode != null && barcodes.Contains(c.Barcode)) || copyIds.Contains(c.CopyId))
            .ToListAsync();

        if (copies.Count != dto.Copies.Count)
            return BadRequest(new { message = "Một hoặc nhiều bản sao không tồn tại" });

        // Validate each copy
        foreach (var copy in copies)
        {
            if (copy.Status != BookCopyStatus.Available)
                return BadRequest(new { message = $"Bản sao '{copy.Barcode}' không khả dụng (trạng thái: {copy.Status})" });

            if (copy.IsReferenceOnly)
                return BadRequest(new { message = $"Bản sao '{copy.Barcode}' chỉ dùng để tham khảo, không cho mượn" });
        }

        // Check duplicates in the batch
        var duplicateCopyIds = copies.GroupBy(c => c.CopyId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateCopyIds.Any())
            return BadRequest(new { message = "Có bản sao bị trùng trong danh sách" });

        // Get the number of books currently borrowed
        var borrowingCount = await _context.Transactions
            .CountAsync(t => t.UserId == dto.UserId &&
                        (t.Status == TransactionStatus.Borrowed || t.Status == TransactionStatus.Overdue));

        // Check the limits for each type of document
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == dto.UserId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var copiesByDocType = copies.GroupBy(c => c.Book!.DocumentTypeId);
        foreach (var group in copiesByDocType)
        {
            var policy = await _context.BorrowPolicies
                .FirstOrDefaultAsync(p => userRoles.Contains(p.AspNetRoleId) && p.DocumentTypeId == group.Key);

            if (policy != null)
            {
                // Count the borrowed items of the same type
                var currentBorrowingOfType = await _context.Transactions
                    .Include(t => t.Copy).ThenInclude(c => c!.Book)
                    .Where(t => t.UserId == dto.UserId &&
                            (t.Status == TransactionStatus.Borrowed || t.Status == TransactionStatus.Overdue) &&
                            t.Copy!.Book!.DocumentTypeId == group.Key)
                    .CountAsync();

                if (currentBorrowingOfType + group.Count() > policy.MaxBooks)
                    return BadRequest(new
                    {
                        message = $"Vượt giới hạn mượn cho loại tài liệu. Đang mượn {currentBorrowingOfType}/{policy.MaxBooks}, thêm {group.Count()} cuốn sẽ vượt quá"
                    });
            }
        }

        // Create Transactions
        var librarianId  = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var now          = DateTime.Now;
        var transactions = new List<Transaction>();

        foreach (var copy in copies)
        {
            var policy = await _context.BorrowPolicies
                .FirstOrDefaultAsync(p => userRoles.Contains(p.AspNetRoleId) && p.DocumentTypeId == copy.Book!.DocumentTypeId);

            var borrowDays = policy?.MaxBorrowDays ?? 14;

            var transaction = new Transaction
            {
                UserId      = dto.UserId,
                CopyId      = copy.CopyId,
                LibrarianId = librarianId,
                RequestId   = dto.RequestId,
                BorrowDate  = now,
                DueDate     = now.AddDays(borrowDays),
                Status      = TransactionStatus.Borrowed,
                Notes       = dto.Notes
            };

            copy.Status = BookCopyStatus.Borrowed;
            _context.Transactions.Add(transaction);
            transactions.Add(transaction);
        }

        // Mark the request as processed if there is one
        if (request != null)
            request.Status = RequestStatus.Completed;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Cho mượn thành công {transactions.Count} cuốn",
            transactions = transactions.Select(t => new
            {
                t.TransactionId,
                t.BorrowDate,
                t.DueDate,
                CopyBarcode = copies.First(c => c.CopyId == t.CopyId).Barcode,
                BookTitle   = copies.First(c => c.CopyId == t.CopyId).Book?.Title
            })
        });
    }

    // GET /api/Transactions/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetById(int id)
    {
        var t = await _context.Transactions
            .Include(t => t.User)
                .ThenInclude(u => u.StudentProfile)
            .Include(t => t.Copy)
                .ThenInclude(c => c.Book)
            .Include(t => t.Fines)
            .FirstOrDefaultAsync(t => t.TransactionId == id);

        if (t == null)
            return NotFound(new { message = "Không tìm thấy giao dịch" });

        var overdueDays = t.DueDate < DateTime.Now && t.ReturnDate == null
            ? (int)(DateTime.Now - t.DueDate).TotalDays : 0;

        // Estimated fine
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == t.UserId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var policy = t.Copy?.Book != null
            ? await _context.BorrowPolicies
                .Where(p => userRoles.Contains(p.AspNetRoleId) && p.DocumentTypeId == t.Copy.Book.DocumentTypeId)
                .FirstOrDefaultAsync()
            : null;

        var estimatedFine = overdueDays > 0 && policy != null
            ? overdueDays * policy.FinePerDay : 0;

        return Ok(new
        {
            t.TransactionId,
            t.BorrowDate,
            t.DueDate,
            t.ReturnDate,
            t.Status,
            t.Notes,
            t.ReturnCondition,
            OverdueDays   = overdueDays,
            EstimatedFine = estimatedFine,
            FinePerDay    = policy?.FinePerDay ?? 0,
            BookPrice     = t.Copy.Book?.Price,
            User = new
            {
                t.User.Id,
                t.User.FullName,
                t.User.Email,
                StudentCode = t.User.StudentProfile?.StudentCode
            },
            Copy = t.Copy == null ? null : new
            {
                t.Copy.CopyId,
                t.Copy.Barcode,
                t.Copy.ShelfLocation,
                BookTitle      = t.Copy.Book?.Title,
                BookId         = t.Copy.Book?.BookId,
                DocumentTypeId = t.Copy.Book?.DocumentTypeId
            },
            Fines = t.Fines.Select(f => new
            {
                f.FineId,
                f.Amount,
                f.Reason,
                f.Status,
                f.CreatedDate,
                f.PaidDate
            })
        });
    }

    // POST /api/Transactions/{id}/return — handle book returns
    [HttpPost("{id}/return")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> ReturnBook(int id, [FromBody] ReturnBookDto dto)
    {
        var transaction = await _context.Transactions
            .Include(t => t.User)
            .Include(t => t.Copy)
                .ThenInclude(c => c.Book)
            .Include(t => t.Fines)
            .FirstOrDefaultAsync(t => t.TransactionId == id);

        if (transaction == null)
            return NotFound(new { message = "Không tìm thấy giao dịch" });

        if (transaction.Status == TransactionStatus.Returned)
            return BadRequest(new { message = "Sách đã được trả trước đó" });

        if (transaction.Status == TransactionStatus.Cancelled)
            return BadRequest(new { message = "Giao dịch đã bị huỷ" });

        var librarianId  = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var returnDate   = DateTime.Now;
        var overdueDays  = transaction.DueDate < returnDate
            ? (int)(returnDate - transaction.DueDate).TotalDays : 0;

        transaction.ReturnDate          = returnDate;
        transaction.ReturnCondition     = dto.ReturnCondition;
        transaction.ReturnLibrarianId   = librarianId;
        transaction.Status              = TransactionStatus.Returned;
        transaction.Notes               = dto.Notes ?? transaction.Notes;

        // Update copy status
        if (transaction.Copy != null)
        {
            transaction.Copy.Status = dto.ReturnCondition == "Hư hỏng" ? BookCopyStatus.Damaged
                                    : dto.ReturnCondition == "Mất"     ? BookCopyStatus.Lost
                                    : BookCopyStatus.Available;

            if (dto.ReturnCondition != null && dto.ReturnCondition != "Tốt" && dto.ReturnCondition != "Bình thường")
            {
                _context.BookCopyStatusHistories.Add(new BookCopyStatusHistory
                {
                    CopyId      = transaction.Copy.CopyId,
                    OldStatus   = BookCopyStatus.Borrowed.ToString(),
                    NewStatus   = transaction.Copy.Status.ToString(),
                    ChangedById = librarianId,
                    ChangedAt   = DateTime.Now,
                    Reason      = $"Trả sách - tình trạng: {dto.ReturnCondition}"
                });
            }
        }

        // Create a fine if overdue or damaged/lost
        var fines = new List<Fine>();

        if (overdueDays > 0 && dto.CreateOverdueFine)
        {
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == transaction.UserId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            var policy = transaction.Copy?.Book != null
                ? await _context.BorrowPolicies
                    .Where(p => userRoles.Contains(p.AspNetRoleId) &&
                                p.DocumentTypeId == transaction.Copy.Book.DocumentTypeId)
                    .FirstOrDefaultAsync()
                : null;

            var fineAmount = dto.OverdueFineAmount ?? (overdueDays * (policy?.FinePerDay ?? 0));

            if (fineAmount > 0)
            {
                var overdueFine = new Fine
                {
                    TransactionId = id,
                    Amount        = fineAmount,
                    Reason        = $"Quá hạn {overdueDays} ngày",
                    Status        = FineStatus.Pending,
                    CreatedDate   = DateTime.Now
                };
                fines.Add(overdueFine);
                _context.Fines.Add(overdueFine);
            }
        }

        if (dto.ReturnCondition == "Hư hỏng" && dto.DamageFineAmount.HasValue && dto.DamageFineAmount > 0)
        {
            var damageFine = new Fine
            {
                TransactionId = id,
                Amount        = dto.DamageFineAmount.Value,
                Reason        = "Sách bị hư hỏng",
                Note          = dto.DamageNote,
                Status        = FineStatus.Pending,
                CreatedDate   = DateTime.Now
            };
            fines.Add(damageFine);
            _context.Fines.Add(damageFine);
        }

        if (dto.ReturnCondition == "Mất" && dto.LostFineAmount.HasValue && dto.LostFineAmount > 0)
        {
            var lostFine = new Fine
            {
                TransactionId = id,
                Amount        = dto.LostFineAmount.Value,
                Reason        = "Sách bị mất",
                Note          = dto.LostNote,
                Status        = FineStatus.Pending,
                CreatedDate   = DateTime.Now
            };
            fines.Add(lostFine);
            _context.Fines.Add(lostFine);
        }

        await _context.SaveChangesAsync();
        if (fines.Any())
        {
            var totalAmount = fines.Sum(f => f.Amount);
            var reasons     = string.Join(", ", fines.Select(f => f.Reason));

            await _notificationService.CreateAsync(
                transaction.UserId,
                "Bạn có phiếu phạt mới",
                NotificationType.FineCreated,
                $"Phiếu phạt {totalAmount:N0} VNĐ cho sách \"{transaction.Copy?.Book?.Title}\". Lý do: {reasons}",
                "/my-fines"
            );

            _ = _emailService.SendFineCreatedAsync(
                transaction.User!.Email!,
                transaction.User.FullName ?? "",
                transaction.Copy!.Book!.Title,
                totalAmount,
                reasons
            ).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"Email error: {t.Exception?.Message}");
            });
        }
        return Ok(new
        {
            message        = "Trả sách thành công",
            transactionId  = id,
            returnDate,
            overdueDays,
            finesCreated   = fines.Count,
            totalFineAmount = fines.Sum(f => f.Amount)
        });
    }

    // POST /api/Transactions/{id}/extend
    [HttpPost("{id}/extend")]
    [Authorize] // both users and librarians can use it
    public async Task<IActionResult> Extend(int id)
    {
        var transaction = await _context.Transactions
            .Include(t => t.User)
            .Include(t => t.Copy)
                .ThenInclude(c => c.Book)
            .Include(t => t.Fines)
            .FirstOrDefaultAsync(t => t.TransactionId == id);

        if (transaction == null)
            return NotFound(new { message = "Không tìm thấy giao dịch" });

        // Only allow the owner to renew
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isLibrarian   = User.IsInRole("Librarian") || User.IsInRole("Admin");

        if (!isLibrarian && transaction.UserId != currentUserId)
            return Forbid();

        // Check status
        if (transaction.Status == TransactionStatus.Returned)
            return BadRequest(new { message = "Sách đã được trả, không thể gia hạn" });

        if (transaction.Status == TransactionStatus.Overdue)
            return BadRequest(new { message = "Sách đã quá hạn, không thể gia hạn. Vui lòng đến thư viện để xử lý" });

        if (transaction.Status == TransactionStatus.Cancelled)
            return BadRequest(new { message = "Giao dịch đã bị huỷ" });

        // Check if there are any unpaid fines left
        var hasPendingFine = transaction.Fines.Any(f => f.Status == FineStatus.Pending);
        if (hasPendingFine)
            return BadRequest(new { message = "Bạn còn phiếu phạt chưa thanh toán, không thể gia hạn" });

        // Get policy
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == transaction.UserId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var policy = transaction.Copy?.Book != null
            ? await _context.BorrowPolicies
                .FirstOrDefaultAsync(p =>
                    userRoles.Contains(p.AspNetRoleId) &&
                    p.DocumentTypeId == transaction.Copy.Book.DocumentTypeId)
            : null;

        var maxExtension  = policy?.MaxExtention ?? 1;
        var borrowDays    = policy?.MaxBorrowDays ?? 14;

        // Check the number of extend
        if (transaction.ExtensionCount >= maxExtension)
            return BadRequest(new
            {
                message = $"Đã hết lượt gia hạn (tối đa {maxExtension} lần)"
            });

        // extend
        var oldDueDate = transaction.DueDate;
        transaction.DueDate        = transaction.DueDate.AddDays(borrowDays);
        transaction.ExtensionCount += 1;

        await _context.SaveChangesAsync();

        // Send a confirmation email
        if (!string.IsNullOrWhiteSpace(transaction.User?.Email))
        {
            _ = _emailService.SendAsync(
                transaction.User.Email,
                "Xác nhận gia hạn mượn sách",
                $"""
                <div style="font-family: Arial, sans-serif; max-width: 600px;">
                <h2 style="color: #2e7d32;">Gia hạn mượn sách thành công ✓</h2>
                <p>Xin chào <strong>{transaction.User.FullName}</strong>,</p>
                <p>Sách của bạn đã được gia hạn:</p>
                <table style="width:100%; border-collapse:collapse; margin:16px 0;">
                    <tr style="background:#f5f5f5;">
                    <td style="padding:10px; border:1px solid #ddd;">Sách</td>
                    <td style="padding:10px; border:1px solid #ddd;">
                        <strong>{transaction.Copy?.Book?.Title}</strong>
                    </td>
                    </tr>
                    <tr>
                    <td style="padding:10px; border:1px solid #ddd;">Hạn cũ</td>
                    <td style="padding:10px; border:1px solid #ddd;">{oldDueDate:dd/MM/yyyy}</td>
                    </tr>
                    <tr style="background:#e8f5e9;">
                    <td style="padding:10px; border:1px solid #ddd;">Hạn mới</td>
                    <td style="padding:10px; border:1px solid #ddd; color:#2e7d32;">
                        <strong>{transaction.DueDate:dd/MM/yyyy}</strong>
                    </td>
                    </tr>
                    <tr>
                    <td style="padding:10px; border:1px solid #ddd;">Lượt gia hạn còn lại</td>
                    <td style="padding:10px; border:1px solid #ddd;">
                        {maxExtension - transaction.ExtensionCount} / {maxExtension}
                    </td>
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

        return Ok(new
        {
            message            = "Gia hạn thành công",
            transactionId      = transaction.TransactionId,
            oldDueDate,
            newDueDate         = transaction.DueDate,
            extensionCount     = transaction.ExtensionCount,
            extensionsRemaining = maxExtension - transaction.ExtensionCount
        });
    }
}

// DTOs
public class CreateTransactionDto
{
    public string UserId { get; set; } = "";
    public int? CopyId { get; set; }
    public string? Barcode { get; set; }
    public int? RequestId { get; set; }
    public string? Notes { get; set; }
}

public class ReturnBookDto
{
    public string ReturnCondition { get; set; } = "Tốt"; // Tốt / Bình thường / Hư hỏng / Mất
    public string? Notes { get; set; }
    public bool CreateOverdueFine { get; set; } = true;
    public decimal? OverdueFineAmount { get; set; }  // override automatic setting if available
    public decimal? DamageFineAmount { get; set; }
    public string? DamageNote { get; set; }
    public decimal? LostFineAmount { get; set; }
    public string? LostNote { get; set; }
}
public class CreateBatchTransactionDto
{
    public string UserId { get; set; } = "";
    public int? RequestId { get; set; }
    public string? Notes { get; set; }
    public List<CopyItemDto> Copies { get; set; } = new();
}

public class CopyItemDto
{
    public int? CopyId { get; set; }
    public string? Barcode { get; set; }
}