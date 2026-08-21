using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/Users")]
[Authorize(Roles = "Admin,Librarian")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET /api/Users?role=Student&status=Active&search=nguyen&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Get userId by role if there's a filter
        IList<string>? userIdsByRole = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            userIdsByRole = usersInRole.Select(u => u.Id).ToList();
        }

        var query = _context.Users
            .Include(u => u.StudentProfile)
            .Include(u => u.StaffProfile)
            .AsQueryable();

        if (userIdsByRole != null)
            query = query.Where(u => userIdsByRole.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status, out var statusEnum))
            query = query.Where(u => u.Status == statusEnum);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                (u.FullName != null && u.FullName.Contains(search)) ||
                (u.Email != null && u.Email.Contains(search)) ||
                (u.StudentProfile != null && u.StudentProfile.StudentCode.Contains(search)) ||
                (u.StudentProfile != null && u.StudentProfile.Term != null && u.StudentProfile.Term.Contains(search)) ||
                (u.StudentProfile != null && u.StudentProfile.Class != null && u.StudentProfile.Class.Contains(search)) ||
                (u.StudentProfile != null && u.StudentProfile.Faculty != null && u.StudentProfile.Faculty.Contains(search)) ||
                (u.StaffProfile != null && u.StaffProfile.StaffCode.Contains(search)) ||
                (u.StaffProfile != null && u.StaffProfile.Department != null && u.StaffProfile.Department.Contains(search)));

        var total = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.UserName,
                u.Status,
                u.CreatedDate,
                u.LastLogin,
                u.ExpiredDate,
                StudentProfile = u.StudentProfile == null ? null : new
                {
                    u.StudentProfile.StudentCode,
                    u.StudentProfile.Class,
                    u.StudentProfile.Faculty,
                    u.StudentProfile.Major,
                    u.StudentProfile.Term,
                    u.StudentProfile.AdmissionYear
                },
                StaffProfile = u.StaffProfile == null ? null : new
                {
                    u.StaffProfile.StaffCode,
                    u.StaffProfile.Position,
                    u.StaffProfile.Department
                },
                // Borrowing books
                BorrowingCount = _context.Transactions
                    .Count(t => t.UserId == u.Id && t.Status == TransactionStatus.Borrowed),
                // Overdue books
                OverdueCount = _context.Transactions
                    .Count(t => t.UserId == u.Id && t.Status == TransactionStatus.Overdue)
            })
            .ToListAsync();

        var result = new List<object>();
        foreach (var u in users)
        {
            var appUser = await _userManager.FindByIdAsync(u.Id);
            var roles = appUser != null ? await _userManager.GetRolesAsync(appUser) : new List<string>();
            result.Add(new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.UserName,
                u.Status,
                u.CreatedDate,
                u.LastLogin,
                u.ExpiredDate,
                u.StudentProfile,
                u.StaffProfile,
                u.BorrowingCount,
                u.OverdueCount,
                Roles = roles
            });
        }

        return Ok(new
        {
            items = result,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    // GET /api/Users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _context.Users
            .Include(u => u.StudentProfile)
            .Include(u => u.StaffProfile)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound(new { message = "Không tìm thấy người dùng" });

        var roles = await _userManager.GetRolesAsync(user);

        // Statistics
        var stats = await _context.Transactions
            .Where(t => t.UserId == id)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.UserName,
            user.PhoneNumber,
            user.Status,
            user.CreatedDate,
            user.LastLogin,
            user.ExpiredDate,
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
            },
            Roles = roles,
            Stats = stats
        });
    }

    // GET /api/Users/{id}/transactions?page=1&pageSize=10
    [HttpGet("{id}/transactions")]
    public async Task<IActionResult> GetTransactions(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == id);
        if (!userExists)
            return NotFound(new { message = "Không tìm thấy người dùng" });

        var total = await _context.Transactions.CountAsync(t => t.UserId == id);

        var transactions = await _context.Transactions
            .Include(t => t.Copy)
                .ThenInclude(c => c.Book)
            .Where(t => t.UserId == id)
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
                t.Notes,
                BookTitle  = t.Copy != null && t.Copy.Book != null ? t.Copy.Book.Title : null,
                Barcode    = t.Copy != null ? t.Copy.Barcode : null,
                IsOverdue  = t.ReturnDate == null && t.DueDate < DateTime.Now
            })
            .ToListAsync();

        return Ok(new
        {
            items = transactions,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    // PATCH /api/Users/{id}/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateUserStatusDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "Không tìm thấy người dùng" });

        // Librarian can only lock/unlock, cannot switch to Inactive
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUser = await _userManager.FindByIdAsync(currentUserId);
        var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, "Admin");

        if (!isAdmin && dto.Status == UserStatus.Inactive)
            return Forbid();

        // Không cho khóa chính mình
        if (id == currentUserId)
            return BadRequest(new { message = "Không thể thay đổi trạng thái tài khoản của chính mình" });

        // Không cho Librarian khóa Admin
        var targetRoles = await _userManager.GetRolesAsync(user);
        if (!isAdmin && (targetRoles.Contains("Admin") || targetRoles.Contains("Librarian")))
            return Forbid();

        user.Status = dto.Status;
        await _userManager.UpdateAsync(user);

        return Ok(new { id = user.Id, status = user.Status.ToString() });
    }

    // POST /api/Users — tạo tài khoản mới
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return Conflict(new { message = "Email đã tồn tại" });

        var user = new ApplicationUser
        {
            UserName     = dto.Email,
            Email        = dto.Email,
            EmailConfirmed = true,
            FullName     = dto.FullName,
            PhoneNumber  = dto.PhoneNumber,
            Status       = UserStatus.Active,
            CreatedDate  = DateTime.Now,
            ExpiredDate  = dto.ExpiredDate
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });

        await _userManager.AddToRoleAsync(user, dto.Role);

        // Tạo profile theo role
        if (dto.Role == "Student" && dto.StudentProfile != null)
        {
            _context.StudentProfiles.Add(new StudentProfile
            {
                UserId        = user.Id,
                StudentCode   = dto.StudentProfile.StudentCode,
                Class         = dto.StudentProfile.Class,
                Faculty       = dto.StudentProfile.Faculty,
                Major         = dto.StudentProfile.Major,
                Term          = dto.StudentProfile.Term,
                AdmissionYear = dto.StudentProfile.AdmissionYear
            });
        }
        else if (dto.Role == "Staff" && dto.StaffProfile != null)
        {
            _context.StaffProfiles.Add(new StaffProfile
            {
                UserId     = user.Id,
                StaffCode  = dto.StaffProfile.StaffCode,
                Position   = dto.StaffProfile.Position,
                Department = dto.StaffProfile.Department
            });
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { user.Id, user.FullName, user.Email });
    }

    // PUT /api/Users/{id} — cập nhật thông tin
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto dto)
    {
        var user = await _context.Users
            .Include(u => u.StudentProfile)
            .Include(u => u.StaffProfile)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound(new { message = "Không tìm thấy người dùng" });

        user.FullName    = dto.FullName;
        user.PhoneNumber = dto.PhoneNumber;
        user.ExpiredDate = dto.ExpiredDate;

        if (dto.StudentProfile != null && user.StudentProfile != null)
        {
            user.StudentProfile.StudentCode   = dto.StudentProfile.StudentCode;
            user.StudentProfile.Class         = dto.StudentProfile.Class;
            user.StudentProfile.Faculty       = dto.StudentProfile.Faculty;
            user.StudentProfile.Major         = dto.StudentProfile.Major;
            user.StudentProfile.Term          = dto.StudentProfile.Term;
            user.StudentProfile.AdmissionYear = dto.StudentProfile.AdmissionYear;
        }

        if (dto.StaffProfile != null && user.StaffProfile != null)
        {
            user.StaffProfile.StaffCode  = dto.StaffProfile.StaffCode;
            user.StaffProfile.Position   = dto.StaffProfile.Position;
            user.StaffProfile.Department = dto.StaffProfile.Department;
        }

        bool codeChanged = false;

        if (user.StudentProfile != null && dto.StudentProfile != null && dto.StudentProfile.StudentCode != null &&
        dto.StudentProfile.StudentCode != user.StudentProfile.StudentCode)
        {
            var oldCode = user.StudentProfile.StudentCode;
            user.StudentProfile.StudentCode = dto.StudentProfile.StudentCode;
            codeChanged = true;

            var newPassword = $"{dto.StudentProfile.StudentCode}@Utc1";
            await _userManager.RemovePasswordAsync(user);
            await _userManager.AddPasswordAsync(user, newPassword);
        }

        if (user.StaffProfile != null && dto.StaffProfile != null && dto.StaffProfile.StaffCode != null &&
            dto.StaffProfile.StaffCode != user.StaffProfile.StaffCode)
        {
            user.StaffProfile.StaffCode = dto.StaffProfile.StaffCode;
            codeChanged = true;

            var newPassword = $"{dto.StaffProfile.StaffCode}@Utc1";
            await _userManager.RemovePasswordAsync(user);
            await _userManager.AddPasswordAsync(user, newPassword);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = codeChanged
                ? "Cập nhật thành công. Mật khẩu đã được reset theo mã mới."
                : "Cập nhật thành công",
            codeChanged,
        });
    }

    // POST /api/Users/import
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportUsersDto dto)
    {
        if (dto.Users == null || dto.Users.Count == 0)
            return BadRequest(new { message = "Danh sách trống" });

        if (dto.Role != "Student" && dto.Role != "Staff")
            return BadRequest(new { message = "Role không hợp lệ, chỉ chấp nhận Student hoặc Staff" });

        var results = new List<ImportUserResultDto>();

        foreach (var item in dto.Users)
        {
            var result = new ImportUserResultDto
            {
                Email = item.Email,
                FullName = item.FullName
            };

            try
            {
                // Check email trùng
                var existing = await _userManager.FindByEmailAsync(item.Email);
                if (existing != null)
                {
                    result.Success = false;
                    result.Error = "Email đã tồn tại";
                    results.Add(result);
                    continue;
                }

                // Check mã SV/CB trùng
                if (dto.Role == "Student")
                {
                    var codeExists = await _context.StudentProfiles
                        .AnyAsync(s => s.StudentCode == item.Code);
                    if (codeExists)
                    {
                        result.Success = false;
                        result.Error = $"Mã sinh viên '{item.Code}' đã tồn tại";
                        results.Add(result);
                        continue;
                    }
                }
                else
                {
                    var codeExists = await _context.StaffProfiles
                        .AnyAsync(s => s.StaffCode == item.Code);
                    if (codeExists)
                    {
                        result.Success = false;
                        result.Error = $"Mã cán bộ '{item.Code}' đã tồn tại";
                        results.Add(result);
                        continue;
                    }
                }
                if (dto.Role == "Student" && !item.AdmissionYear.HasValue)
                {
                    result.Success = false;
                    result.Error = "Năm nhập học không được để trống";
                    results.Add(result);
                    continue;
                }

                var user = new ApplicationUser
                {
                    UserName    = item.Email,
                    Email       = item.Email,
                    EmailConfirmed = true,
                    FullName    = item.FullName,
                    PhoneNumber = item.PhoneNumber,
                    Status      = UserStatus.Active,
                    CreatedDate = DateTime.Now,
                    ExpiredDate = dto.Role == "Student" && item.AdmissionYear.HasValue
                        ? new DateTime(item.AdmissionYear.Value + 5, 12, 31) // hết hạn cuối năm tốt nghiệp
                        : dto.BatchExpiredDate  // dùng ngày hết hạn chung cho Staff
                };

                // Mật khẩu mặc định = mã SV/CB
                var defaultPassword = $"{item.Code}@Utc1";
                var createResult = await _userManager.CreateAsync(user, defaultPassword);
                if (!createResult.Succeeded)
                {
                    result.Success = false;
                    result.Error = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    results.Add(result);
                    continue;
                }

                await _userManager.AddToRoleAsync(user, dto.Role);

                if (dto.Role == "Student")
                {
                    _context.StudentProfiles.Add(new StudentProfile
                    {
                        UserId        = user.Id,
                        StudentCode   = item.Code,
                        Class         = item.Class,
                        Faculty       = item.Faculty,
                        Major         = item.Major,
                        Term          = item.Term,
                        AdmissionYear = item.AdmissionYear
                    });
                }
                else
                {
                    _context.StaffProfiles.Add(new StaffProfile
                    {
                        UserId     = user.Id,
                        StaffCode  = item.Code,
                        Position   = item.Position,
                        Department = item.Department
                    });
                }

                await _context.SaveChangesAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            results.Add(result);
        }

        return Ok(new
        {
            total   = results.Count,
            success = results.Count(r => r.Success),
            failed  = results.Count(r => !r.Success),
            results
        });
    }
}

public class UpdateUserStatusDto
{
    public UserStatus Status { get; set; }
}
public class CreateUserDto
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "";
    public DateTime? ExpiredDate { get; set; }
    public CreateStudentProfileDto? StudentProfile { get; set; }
    public CreateStaffProfileDto? StaffProfile { get; set; }
}

public class CreateStudentProfileDto
{
    public string StudentCode { get; set; } = "";
    public string? Class { get; set; }
    public string? Faculty { get; set; }
    public string? Major { get; set; }
    public string? Term { get; set; }
    public int? AdmissionYear { get; set; }
}

public class CreateStaffProfileDto
{
    public string StaffCode { get; set; } = "";
    public string? Position { get; set; }
    public string? Department { get; set; }
}

public class UpdateUserDto
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public CreateStudentProfileDto? StudentProfile { get; set; }
    public CreateStaffProfileDto? StaffProfile { get; set; }
}
public class ImportUsersDto
{
    public string Role { get; set; } = "";
    public DateTime? BatchExpiredDate { get; set; }
    public List<ImportUserItemDto> Users { get; set; } = new();
}

public class ImportUserItemDto
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Code { get; set; } = "";       // StudentCode hoặc StaffCode
    public string? PhoneNumber { get; set; }
    public DateTime? ExpiredDate { get; set; }
    // Student fields
    public string? Class { get; set; }
    public string? Faculty { get; set; }
    public string? Major { get; set; }
    public string? Term { get; set; }
    public int? AdmissionYear { get; set; }
    // Staff fields
    public string? Position { get; set; }
    public string? Department { get; set; }
}

public class ImportUserResultDto
{
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool Success { get; set; }
    public string? Error { get; set; }
}