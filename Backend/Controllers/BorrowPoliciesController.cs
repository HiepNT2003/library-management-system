using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/BorrowPolicies")]
[Authorize(Roles = "Admin,Librarian")]
public class BorrowPoliciesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BorrowPoliciesController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/BorrowPolicies
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var policies = await _context.BorrowPolicies
            .Include(p => p.Role)
            .Include(p => p.DocumentType)
            .OrderBy(p => p.AspNetRoleId)
            .ThenBy(p => p.DocumentTypeId)
            .Select(p => new
            {
                p.Id,
                p.MaxBorrowDays,
                p.MaxBooks,
                p.MaxExtention,
                p.FinePerDay,
                p.FinePaymentDeadlineDays,
                Role = new { p.Role.Id, p.Role.Name },
                DocumentType = new { p.DocumentType.DocumentTypeId, p.DocumentType.Name }
            })
            .ToListAsync();

        // Get the list of roles and document types to display the form
        var roles = await _context.Roles
            .Where(r => r.Name == "Student" || r.Name == "Staff")
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        var documentTypes = await _context.DocumentTypes
            .Where(dt => dt.DocumentTypeId == 1 || dt.DocumentTypeId == 3)
            .Select(dt => new { dt.DocumentTypeId, dt.Name })
            .ToListAsync();

        return Ok(new { policies, roles, documentTypes });
    }

    // POST /api/BorrowPolicies
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertBorrowPolicyDto dto)
    {
        var exists = await _context.BorrowPolicies
            .AnyAsync(p => p.AspNetRoleId == dto.RoleId && p.DocumentTypeId == dto.DocumentTypeId);

        if (exists)
            return Conflict(new { message = "Chính sách cho role và loại tài liệu này đã tồn tại" });

        var policy = new BorrowPolicy
        {
            AspNetRoleId   = dto.RoleId,
            DocumentTypeId = dto.DocumentTypeId,
            MaxBorrowDays  = dto.MaxBorrowDays,
            MaxBooks       = dto.MaxBooks,
            MaxExtention   = dto.MaxExtention,
            FinePerDay     = dto.FinePerDay,
            FinePaymentDeadlineDays = dto.FinePaymentDeadlineDays
        };

        _context.BorrowPolicies.Add(policy);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Tạo chính sách thành công", policy.Id });
    }

    // PUT /api/BorrowPolicies/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertBorrowPolicyDto dto)
    {
        var policy = await _context.BorrowPolicies.FindAsync(id);
        if (policy == null)
            return NotFound(new { message = "Không tìm thấy chính sách" });

        policy.MaxBorrowDays = dto.MaxBorrowDays;
        policy.MaxBooks      = dto.MaxBooks;
        policy.MaxExtention  = dto.MaxExtention;
        policy.FinePerDay    = dto.FinePerDay;
        policy.FinePaymentDeadlineDays = dto.FinePaymentDeadlineDays;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cập nhật thành công" });
    }

    // DELETE /api/BorrowPolicies/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var policy = await _context.BorrowPolicies.FindAsync(id);
        if (policy == null)
            return NotFound(new { message = "Không tìm thấy chính sách" });

        _context.BorrowPolicies.Remove(policy);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class UpsertBorrowPolicyDto
{
    public string RoleId { get; set; } = "";
    public int DocumentTypeId { get; set; }
    public int MaxBorrowDays { get; set; }
    public int MaxBooks { get; set; }
    public int MaxExtention { get; set; }
    public decimal FinePerDay { get; set; }
    public int FinePaymentDeadlineDays { get; set; } = 7;
}