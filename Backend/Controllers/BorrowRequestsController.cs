using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("api/BorrowRequests")]
public class BorrowRequestsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    public BorrowRequestsController(AppDbContext context, IEmailService emailService, INotificationService notificationService)
    {
        _context = context;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    // GET /api/BorrowRequests?status=Pending&search=&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.BorrowRequests
            .Include(r => r.User)
                .ThenInclude(u => u.StudentProfile)
            .Include(r => r.User)
                .ThenInclude(u => u.StaffProfile)
            .Include(r => r.Book)
            .Where(r => !r.Book.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RequestStatus>(status, out var statusEnum))
            query = query.Where(r => r.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r =>
                (r.User.FullName != null && r.User.FullName.Contains(search)) ||
                (r.User.StudentProfile != null && r.User.StudentProfile.StudentCode.Contains(search)) ||
                (r.Book.Title != null && r.Book.Title.Contains(search)));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.RequestDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.RequestId,
                r.RequestDate,
                r.ExpectedBorrowDate,
                r.Status,
                r.Note,
                r.RejectedReason,
                r.ApprovedDate,
                Book = new { r.Book.BookId, r.Book.Title, r.Book.ImageUrl, r.Book.DocumentTypeId },
                User = new
                {
                    r.User.Id,
                    r.User.FullName,
                    r.User.Email,
                    StudentCode = r.User.StudentProfile != null ? r.User.StudentProfile.StudentCode : null,
                    StaffCode   = r.User.StaffProfile   != null ? r.User.StaffProfile.StaffCode     : null,
                    Faculty     = r.User.StudentProfile != null ? r.User.StudentProfile.Faculty
                                : r.User.StaffProfile   != null ? r.User.StaffProfile.Department    : null
                }
            })
            .ToListAsync();

        // Count by status
        var stats = await _context.BorrowRequests
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
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

    // GET /api/BorrowRequests/{id}
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _context.BorrowRequests
            .Include(r => r.User)
                .ThenInclude(u => u.StudentProfile)
            .Include(r => r.User)
                .ThenInclude(u => u.StaffProfile)
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu" });

        // Check the number of books the user is borrowing
        var borrowingCount = await _context.Transactions
            .CountAsync(t => t.UserId == request.UserId &&
                        (t.Status == TransactionStatus.Borrowed || t.Status == TransactionStatus.Overdue));

        // Get BorrowPolicy
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == request.UserId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var policy = await _context.BorrowPolicies
            .Where(p => userRoles.Contains(p.AspNetRoleId) && p.DocumentTypeId == request.Book.DocumentTypeId)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            request.RequestId,
            request.RequestDate,
            request.ExpectedBorrowDate,
            request.Status,
            request.Note,
            request.RejectedReason,
            request.ApprovedDate,
            request.ApprovedBy,
            Book = new { request.Book.BookId, request.Book.Title, request.Book.ImageUrl, request.Book.DocumentTypeId },
            User = new
            {
                request.User.Id,
                request.User.FullName,
                request.User.Email,
                request.User.PhoneNumber,
                StudentCode    = request.User.StudentProfile?.StudentCode,
                Faculty        = request.User.StudentProfile?.Faculty ?? request.User.StaffProfile?.Department,
                BorrowingCount = borrowingCount
            },
            Policy = policy == null ? null : new
            {
                policy.MaxBorrowDays,
                policy.MaxBooks,
                policy.MaxExtention,
                policy.FinePerDay
            }
        });
    }

    // PATCH /api/BorrowRequests/{id}/approve
    [HttpPatch("{id}/approve")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Approve(int id)
    {
        var request = await _context.BorrowRequests
            .Include(r => r.Book)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu" });

        if (request.Status != RequestStatus.Pending)
            return BadRequest(new { message = "Chỉ có thể duyệt yêu cầu đang chờ xử lý" });

        // Check if the book has a copy available
        var availableCopy = await _context.BookCopies
            .Where(c => c.BookId == request.BookId &&
                        c.Status == BookCopyStatus.Available &&
                        !c.IsReferenceOnly)
            .FirstOrDefaultAsync();

        if (availableCopy == null)
            return BadRequest(new { message = "Không có bản sao nào khả dụng để duyệt yêu cầu này" });

        // Check borrowing limit
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == request.UserId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var policy = await _context.BorrowPolicies
            .Where(p => userRoles.Contains(p.AspNetRoleId) && p.DocumentTypeId == request.Book.DocumentTypeId)
            .FirstOrDefaultAsync();

        if (policy != null)
        {
            var borrowingCount = await _context.Transactions
                .CountAsync(t => t.UserId == request.UserId &&
                            (t.Status == TransactionStatus.Borrowed || t.Status == TransactionStatus.Overdue));

            if (borrowingCount >= policy.MaxBooks)
                return BadRequest(new { message = $"Bạn đọc đã đạt giới hạn {policy.MaxBooks} cuốn sách đang mượn" });
        }

        var librarianId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        request.Status      = RequestStatus.Approved;
        request.ApprovedDate = DateTime.Now;
        request.ApprovedBy  = librarianId;

        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(
            request.UserId,
            "Yêu cầu mượn sách được duyệt ✓",
            NotificationType.BorrowApproved,
            $"Yêu cầu mượn \"{request.Book.Title}\" đã được duyệt. Vui lòng đến thư viện lấy sách.",
            $"/my-requests"
        );

        if (!string.IsNullOrWhiteSpace(request.User?.Email))
        {
            _ = _emailService.SendBorrowApprovedAsync(
                request.User.Email,
                request.User.FullName ?? "",
                request.Book.Title,
                DateTime.Now.AddDays(policy?.MaxBorrowDays ?? 14),
                request.RequestId 
            ).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"Email error: {t.Exception?.Message}");
            });
        }

        return Ok(new { message = "Duyệt yêu cầu thành công", requestId = request.RequestId });
    }

    // PATCH /api/BorrowRequests/{id}/reject
    [HttpPatch("{id}/reject")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectRequestDto dto)
    {
        var request = await _context.BorrowRequests
            .Include(r => r.User)
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu" });

        if (request.Status != RequestStatus.Pending)
            return BadRequest(new { message = "Chỉ có thể từ chối yêu cầu đang chờ xử lý" });

        request.Status         = RequestStatus.Rejected;
        request.RejectedReason = dto.Reason;

        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(
            request.UserId,
            "Yêu cầu mượn sách không được duyệt",
            NotificationType.BorrowRejected,
            $"Yêu cầu mượn \"{request.Book.Title}\" không được duyệt. Lý do: {dto.Reason}",
            $"/my-requests"
        );

        if (!string.IsNullOrWhiteSpace(request.User?.Email))
        {
            _ = _emailService.SendBorrowRejectedAsync(
                request.User.Email,
                request.User.FullName ?? "",
                request.Book.Title,
                dto.Reason ?? ""
            ).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"Email error: {t.Exception?.Message}");
            });
        }
        return Ok(new { message = "Đã từ chối yêu cầu" });
    }

    // GET /api/BorrowRequests/policy?userId=xxx&documentTypeId=1
    [HttpGet("policy")]
    [Authorize]
    public async Task<IActionResult> GetPolicy([FromQuery] string userId, [FromQuery] int documentTypeId)
    {
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var policy = await _context.BorrowPolicies
            .Where(p => userRoles.Contains(p.AspNetRoleId) && p.DocumentTypeId == documentTypeId)
            .Select(p => new {
                p.MaxBorrowDays,
                p.MaxBooks,
                p.MaxExtention,
                p.FinePerDay
            })
            .FirstOrDefaultAsync();

        if (policy == null)
            return Ok(new { maxBorrowDays = 14, maxBooks = 3, maxExtention = 1, finePerDay = 2000 });

        return Ok(policy);
    }

    // GET /api/BorrowRequests/{id}/for-checkout — lấy thông tin khi thủ thư quét QR
    [HttpGet("{id}/for-checkout")]
    [Authorize]
    public async Task<IActionResult> GetForCheckout(int id)
    {
        var request = await _context.BorrowRequests
            .Include(r => r.User)
                .ThenInclude(u => u.StudentProfile)
            .Include(r => r.User)
                .ThenInclude(u => u.StaffProfile)
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.RequestId == id);

        if (request == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu" });

        if (request.Status != RequestStatus.Approved)
            return BadRequest(new { message = "Yêu cầu chưa được duyệt hoặc đã xử lý" });

        // Check user
        if (request.User.Status == UserStatus.Blocked)
            return BadRequest(new { message = "Tài khoản bạn đọc đã bị khóa" });

        return Ok(new
        {
            request.RequestId,
            request.Status,
            User = new
            {
                request.User.Id,
                request.User.FullName,
                request.User.Email,
                request.User.Status,
                request.User.ExpiredDate,
                StudentProfile = request.User.StudentProfile == null ? null : new
                {
                    request.User.StudentProfile.StudentCode
                },
                StaffProfile = request.User.StaffProfile == null ? null : new
                {
                    request.User.StaffProfile.StaffCode
                }
            },
            Book = new
            {
                request.Book.BookId,
                request.Book.Title,
                request.Book.DocumentTypeId,
                request.Book.ImageUrl
            }
        });
    }

    // POST /api/BorrowRequests — Student create request
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateBorrowRequestDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Unauthorized();

        var book = await _context.Books.FindAsync(dto.BookId);
        if (book == null || book.IsDeleted)
            return NotFound(new { message = "Không tìm thấy sách" });

        // Only physics books and theses are allowed to be borrowed
        if (book.DocumentTypeId != 1 && book.DocumentTypeId != 3)
            return BadRequest(new { message = "Loại tài liệu này không hỗ trợ mượn" });

        if (dto.ExpectedBorrowDate.HasValue)
        {
            var minDate = DateTime.Today.AddDays(1);
            var maxDate = DateTime.Today.AddDays(7);

            if (dto.ExpectedBorrowDate.Value.Date < minDate)
                return BadRequest(new { message = "Ngày lấy sách phải là ngày mai trở đi" });

            if (dto.ExpectedBorrowDate.Value.Date > maxDate)
                return BadRequest(new { message = "Ngày lấy sách không được quá 7 ngày kể từ hôm nay" });
        }
        
        // Check if there's already a pending request
        var existing = await _context.BorrowRequests
            .AnyAsync(r => r.UserId == userId && r.BookId == dto.BookId &&
                    r.Status == RequestStatus.Pending);
        if (existing)
            return BadRequest(new { message = "Bạn đã có yêu cầu mượn sách này đang chờ duyệt" });

        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var fineDeadlineDays = await _context.BorrowPolicies
            .Where(p => userRoles.Contains(p.AspNetRoleId))
            .Select(p => p.FinePaymentDeadlineDays)
            .FirstOrDefaultAsync();

        if (fineDeadlineDays == 0) fineDeadlineDays = 7;

        var hasOverdueFine = await _context.Fines
            .AnyAsync(f => f.Transaction.UserId == userId &&
                        f.Status == FineStatus.Pending &&
                        f.CreatedDate < DateTime.Today.AddDays(-fineDeadlineDays));

        if (hasOverdueFine)
            return BadRequest(new
            {
                message = $"Bạn có phiếu phạt quá {fineDeadlineDays} ngày chưa thanh toán. Vui lòng đến thư viện thanh toán trước khi đặt mượn sách."
            });

        var reqPolicy = await _context.BorrowPolicies
            .FirstOrDefaultAsync(p => userRoles.Contains(p.AspNetRoleId)
                                    && p.DocumentTypeId == book.DocumentTypeId);

        if (reqPolicy != null)
        {
            var currentBorrowingOfType = await _context.Transactions
                .Include(t => t.Copy).ThenInclude(c => c!.Book)
                .CountAsync(t => t.UserId == userId &&
                                (t.Status == TransactionStatus.Borrowed ||
                                t.Status == TransactionStatus.Overdue) &&
                                t.Copy!.Book!.DocumentTypeId == book.DocumentTypeId);

            var pendingOfType = await _context.BorrowRequests
                .CountAsync(r => r.UserId == userId &&
                                (r.Status == RequestStatus.Pending ||
                                r.Status == RequestStatus.Approved) &&
                                r.Book!.DocumentTypeId == book.DocumentTypeId);

            if (currentBorrowingOfType + pendingOfType >= reqPolicy.MaxBooks)
                return BadRequest(new
                {
                    message = $"Bạn đã đạt giới hạn {reqPolicy.MaxBooks} cuốn cho loại tài liệu này."
                });
        }

        var availableCopies = await _context.BookCopies
            .CountAsync(c => c.BookId == dto.BookId &&
                            c.Status == BookCopyStatus.Available &&
                            !c.IsReferenceOnly);

        // Count the Pending + Approved requests for this book
        var pendingCount = await _context.BorrowRequests
            .CountAsync(r => r.BookId == dto.BookId &&
                            (r.Status == RequestStatus.Pending ||
                            r.Status == RequestStatus.Approved));

        if (pendingCount >= availableCopies)
            return BadRequest(new { message = "Sách hiện không còn bản sao khả dụng để đặt mượn" });

        var request = new BorrowRequest
        {
            UserId             = userId!,
            BookId             = dto.BookId,
            RequestDate        = DateTime.Now,
            Status             = RequestStatus.Pending,
            Note               = dto.Note,
            ExpectedBorrowDate = dto.ExpectedBorrowDate
        };

        _context.BorrowRequests.Add(request);
        await _context.SaveChangesAsync();

        await _notificationService.CreateForRoleAsync(
            "Librarian",
            "Yêu cầu mượn sách mới",
            NotificationType.NewRequest,
            $"{user.FullName} đã đặt mượn \"{book.Title}\"",
            "/admin/borrow-requests"
        );

        return Ok(new { message = "Gửi yêu cầu thành công", requestId = request.RequestId });
    }
}

public class RejectRequestDto
{
    public string? Reason { get; set; }
}

public class CreateBorrowRequestDto
{
    public int BookId { get; set; }
    public string? Note { get; set; }
    public DateTime? ExpectedBorrowDate { get; set; }
}