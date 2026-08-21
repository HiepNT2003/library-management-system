using Backend.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/account")]

public class AccountController: ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _context;
    public AccountController(UserManager<ApplicationUser> userManager, AppDbContext context) {
        _userManager = userManager;
        _context = context;
    }
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if(userId == null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var studentProfile = await _context.StudentProfiles
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        var staffProfile = await _context.StaffProfiles
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        var userCode = studentProfile?.StudentCode ?? staffProfile?.StaffCode;

        return Ok(new
        {
            user.Id,
            UserName = user.FullName ?? user.UserName,
            user.FullName,
            user.Email,
            roles,
            UserCode = userCode
        });
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents(
        string? keyword,
        string? sortBy = "FullName",
        string? sortOrder = "asc",
        int page = 1,
        int pageSize = 10
    )
    {
        var query = from u in _context.Users
                    join ur in _context.UserRoles on u.Id equals ur.UserId
                    join r in _context.Roles on ur.RoleId equals r.Id
                    where r.Name == "Student"
                    select new
                    {
                        u.Id,
                        u.FullName,
                        u.Email,
                        u.PhoneNumber,
                        StudentCode = u.StudentProfile.StudentCode,
                        Class = u.StudentProfile.Class,
                        Term = u.StudentProfile.Term,
                        Faculty = u.StudentProfile.Faculty,
                        u.ExpiredDate,
                        u.Status,
                        u.CreatedDate
                    };

        // SEARCH
        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(u =>
                u.FullName.Contains(keyword) ||
                u.Email.Contains(keyword) ||
                u.StudentCode.Contains(keyword)
            );
        }

        // SORT
        query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
        {
            ("email", "desc") => query.OrderByDescending(u => u.Email),
            ("email", _) => query.OrderBy(u => u.Email),

            ("createdDate", "desc") => query.OrderByDescending(u => u.CreatedDate),
            ("createdDate", _) => query.OrderBy(u => u.CreatedDate),

            ("studentcode", "desc") => query.OrderByDescending(u => u.StudentCode),
            ("studentcode", _) => query.OrderBy(u => u.StudentCode),

            ("fullname", "desc") => query.OrderByDescending(u => u.FullName),
            _ => query.OrderBy(u => u.FullName),
        };

        // PAGINATION
        var total = await query.CountAsync();

        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var now = DateTime.UtcNow;

        return Ok(new
        {
            data = data.Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.StudentCode,
                u.Class,
                u.Term,
                u.Faculty,
                u.ExpiredDate,
                Status = (u.ExpiredDate.HasValue && u.ExpiredDate < now)
                    ? "Expired"
                    : u.Status?.ToString(),
                u.CreatedDate
            }),
            pagination = new
            {
                page,
                pageSize,
                total,
                totalPages = (int)Math.Ceiling((double)total / pageSize)
            }
        });
    }
    
    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent(CreateStudentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StudentCode))
        {
            return BadRequest("StudentCode is required");
        }

        var existedStudentCode = await _context.StudentProfiles
            .AnyAsync(x => x.StudentCode == request.StudentCode);

        if (existedStudentCode)
        {
            return BadRequest("StudentCode already exists");
        }

        ApplicationUser? existedUser = null;

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            existedUser = await _userManager.FindByEmailAsync(request.Email);

            if (existedUser != null)
            {
                return BadRequest("Email already exists");
            }
        }

        var user = new ApplicationUser
        {
            UserName = request.Email ?? request.StudentCode, // required
            Email = request.Email,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Status = request.Status,
            ExpiredDate = request.ExpiredDate
        };

        IdentityResult result;

        if (!string.IsNullOrWhiteSpace(request.StudentCode))
        {
            result = await _userManager.CreateAsync(user, request.StudentCode + "@lms.UTC.edu.vn");
        }
        else
        {
            result = await _userManager.CreateAsync(user);
        }

        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await _userManager.AddToRoleAsync(user, "Student");

        var studentProfile = new StudentProfile
        {
            UserId = user.Id,
            StudentCode = request.StudentCode,
            Class = request.Class,
            Faculty = request.Faculty,
            Term = request.Term
        };

        _context.StudentProfiles.Add(studentProfile);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Student created successfully",
            userId = user.Id
        });
    }

    // GET /api/Users/my-stats
    [HttpGet("my-stats")]
    [Authorize]
    public async Task<IActionResult> GetMyStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var borrowing = await _context.Transactions
            .CountAsync(t => t.UserId == userId && t.Status == TransactionStatus.Borrowed);

        var overdue = await _context.Transactions
            .CountAsync(t => t.UserId == userId && t.Status == TransactionStatus.Overdue);

        var pendingRequests = await _context.BorrowRequests
            .CountAsync(r => r.UserId == userId && r.Status == RequestStatus.Pending);

        var pendingFines = await _context.Fines
            .CountAsync(f => f.Transaction.UserId == userId && f.Status == FineStatus.Pending);

        return Ok(new { borrowing, overdue, pendingRequests, pendingFines });
    }

    // GET /api/account/profile
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var user = await _context.Users
            .Include(u => u.StudentProfile)
            .Include(u => u.StaffProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.Status,
            user.ExpiredDate,
            Roles = roles,
            StudentProfile = user.StudentProfile == null ? null : new
            {
                user.StudentProfile.StudentCode,
                user.StudentProfile.Class,
                user.StudentProfile.Faculty,
                user.StudentProfile.Major,
                user.StudentProfile.Term,
                user.StudentProfile.AdmissionYear
            },
            StaffProfile = user.StaffProfile == null ? null : new
            {
                user.StaffProfile.StaffCode,
                user.StaffProfile.Position,
                user.StaffProfile.Department
            }
        });
    }

    // PUT /api/account/me — cập nhật thông tin cá nhân
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user   = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.FullName    = dto.FullName?.Trim() ?? user.FullName;
        user.PhoneNumber = dto.PhoneNumber?.Trim();

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công" });
    }

    // POST /api/account/me/change-password
    [HttpPost("me/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user   = await _userManager.FindByIdAsync(userId!);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Errors.FirstOrDefault()?.Description ?? "Đổi mật khẩu thất bại" });

        return Ok(new { message = "Đổi mật khẩu thành công" });
    }

    
    // GET /api/account/me/transactions?status=Borrowed&page=1
    [HttpGet("me/transactions")]
    [Authorize]
    public async Task<IActionResult> GetMyTransactions(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Lazy update overdue
        await _context.Transactions
            .Where(t => t.UserId == userId &&
                        t.Status == TransactionStatus.Borrowed &&
                        t.DueDate < DateTime.Now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TransactionStatus.Overdue));

        var query = _context.Transactions
            .Include(t => t.Copy)
                .ThenInclude(c => c.Book)
                    .ThenInclude(b => b.BookAuthors)
                        .ThenInclude(ba => ba.Author)
            .Include(t => t.Fines)
            .Where(t => t.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TransactionStatus>(status, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);

        var total = await query.CountAsync();

        var rawItems = await query
            .OrderByDescending(t => t.BorrowDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.TransactionId,
                t.BorrowDate,
                t.DueDate,
                t.ReturnDate,
                t.Status,
                t.ExtensionCount,
                Book = t.Copy != null && t.Copy.Book != null ? new
                {
                    t.Copy.Book.BookId,
                    t.Copy.Book.Title,
                    t.Copy.Book.ImageUrl,
                    Authors = t.Copy.Book.BookAuthors
                        .Select(ba => ba.Author.Name)
                } : null,
                Copy = t.Copy != null ? new
                {
                    t.Copy.CopyId,
                    t.Copy.Barcode
                } : null,
                PendingFines = t.Fines
                    .Where(f => f.Status == FineStatus.Pending)
                    .Sum(f => f.Amount),
                HasPendingFine = t.Fines.Any(f => f.Status == FineStatus.Pending)
            })
            .ToListAsync();

        // Caculate OverdueDays client side after fetch 
        var items = rawItems.Select(t => new
        {
            t.TransactionId,
            t.BorrowDate,
            t.DueDate,
            t.ReturnDate,
            t.Status,
            t.ExtensionCount,
            OverdueDays = t.Status == TransactionStatus.Overdue
                ? (int)(DateTime.Now - t.DueDate).TotalDays
                : 0,
            t.Book,
            t.Copy,
            t.PendingFines,
            t.HasPendingFine
        }).ToList();

        // Get policy to caculate maxExtension
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    // GET /api/account/me/requests?status=&page=1
    [HttpGet("me/requests")]
    [Authorize]
    public async Task<IActionResult> GetMyRequests(
        [FromQuery] int? bookId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.BorrowRequests
            .Include(r => r.Book)
                .ThenInclude(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
            .Where(r => r.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<RequestStatus>(status, out var statusEnum))
            query = query.Where(r => r.Status == statusEnum);

        if (bookId.HasValue)
            query = query.Where(r => r.BookId == bookId);

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
                Book = new
                {
                    r.Book.BookId,
                    r.Book.Title,
                    r.Book.ImageUrl,
                    r.Book.DocumentTypeId,
                    Authors = r.Book.BookAuthors.Select(ba => ba.Author.Name)
                }
            })
            .ToListAsync();

        return Ok(new
        {
            items, total, page, pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    // DELETE /api/account/me/requests/{id} — huỷ yêu cầu
    [HttpDelete("me/requests/{id}")]
    [Authorize]
    public async Task<IActionResult> CancelRequest(int id)
    {
        var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var request = await _context.BorrowRequests
            .FirstOrDefaultAsync(r => r.RequestId == id && r.UserId == userId);

        if (request == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu" });

        if (request.Status != RequestStatus.Pending)
            return BadRequest(new { message = "Chỉ có thể huỷ yêu cầu đang chờ duyệt" });

        request.Status = RequestStatus.Cancelled;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã huỷ yêu cầu" });
    }

    // GET /api/account/me/fines?status=&page=1
    [HttpGet("me/fines")]
    [Authorize]
    public async Task<IActionResult> GetMyFines(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.Fines
            .Include(f => f.Transaction)
                .ThenInclude(t => t.Copy)
                    .ThenInclude(c => c.Book)
            .Where(f => f.Transaction.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<FineStatus>(status, out var statusEnum))
            query = query.Where(f => f.Status == statusEnum);

        var total = await query.CountAsync();

        var totalPending = await _context.Fines
            .Where(f => f.Transaction.UserId == userId && f.Status == FineStatus.Pending)
            .SumAsync(f => f.Amount);

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
                Book = f.Transaction.Copy != null && f.Transaction.Copy.Book != null
                    ? new
                    {
                        f.Transaction.Copy.Book.BookId,
                        f.Transaction.Copy.Book.Title,
                        f.Transaction.Copy.Book.ImageUrl
                    } : null,
                Transaction = new
                {
                    f.Transaction.TransactionId,
                    f.Transaction.BorrowDate,
                    f.Transaction.DueDate,
                    f.Transaction.ReturnDate
                }
            })
            .ToListAsync();

        return Ok(new
        {
            items, total, page, pageSize,
            totalPages    = (int)Math.Ceiling((double)total / pageSize),
            totalPending
        });
    }

    // GET /api/account/me/favorites?page=1
    [HttpGet("me/favorites")]
    [Authorize]
    public async Task<IActionResult> GetMyFavorites(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.UserFavoriteBooks
            .Include(f => f.Book)
                .ThenInclude(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
            .Include(f => f.Book)
                .ThenInclude(b => b.BookCategories)
                    .ThenInclude(bc => bc.Category)
            .Where(f => f.UserId == userId && f.Book != null && !f.Book.IsDeleted)
            .OrderByDescending(f => f.CreatedDate);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.Id,
                f.CreatedDate,
                Book = new
                {
                    f.Book!.BookId,
                    f.Book.Title,
                    f.Book.ImageUrl,
                    f.Book.Publisher,
                    f.Book.PublishedYear,
                    f.Book.DocumentTypeId,
                    Authors    = f.Book.BookAuthors.Select(ba => new { ba.Author.AuthorId, ba.Author.Name }),
                    Categories = f.Book.BookCategories.Select(bc => new { bc.Category.CategoryId, bc.Category.Name }),
                    AvailableCopies = f.Book.BookCopies.Count(c => c.Status == BookCopyStatus.Available && !c.IsReferenceOnly),
                    TotalCopies     = f.Book.BookCopies.Count()
                }
            })
            .ToListAsync();

        return Ok(new
        {
            items, total, page, pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    // GET /api/account/me/reading
    [HttpGet("me/reading")]
    [Authorize]
    public async Task<IActionResult> GetMyReading(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var query = _context.ReadingProgresses
            .Include(p => p.Book)
                .ThenInclude(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
            .Where(p => p.UserId == userId &&
                        p.Book != null &&
                        !p.Book.IsDeleted &&
                        p.CurrentPage > 0)
            .OrderByDescending(p => p.LastReadDate);

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.ProgressId,
                p.CurrentPage,
                p.PercentRead,
                p.LastReadDate,
                Book = new
                {
                    p.Book!.BookId,
                    p.Book.Title,
                    p.Book.ImageUrl,
                    p.Book.DocumentTypeId,
                    p.Book.FilePath,
                    Authors = p.Book.BookAuthors.Select(ba => ba.Author.Name)
                }
            })
            .ToListAsync();

        return Ok(new
        {
            items, total, page, pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }
}

public class UpdateMeDto
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}