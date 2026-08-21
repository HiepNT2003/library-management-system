using Backend.Models;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/Dashboard")]
[Authorize(Roles = "Admin,Librarian")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/Dashboard
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var now   = DateTime.Now;
        var today = DateTime.Today;

        // ---- Overview statistics ----
        var totalBooks       = await _context.Books.CountAsync(b => !b.IsDeleted);
        var totalCopies      = await _context.BookCopies.CountAsync();
        var availableCopies  = await _context.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Available);
        var totalUsers       = await _context.Users.CountAsync();
        var activeUsers      = await _context.Users.CountAsync(u => u.Status == UserStatus.Active);
        var borrowing        = await _context.Transactions.CountAsync(t => t.Status == TransactionStatus.Borrowed);
        var overdue          = await _context.Transactions.CountAsync(t => t.Status == TransactionStatus.Overdue);
        var pendingFines     = await _context.Fines.CountAsync(f => f.Status == FineStatus.Pending);
        var pendingFineAmount = await _context.Fines
            .Where(f => f.Status == FineStatus.Pending)
            .SumAsync(f => f.Amount);
        var pendingRequests  = await _context.BorrowRequests
            .CountAsync(r => r.Status == RequestStatus.Pending);

        // ---- Chart of loans and returns over the past 30 days ----
        var thirtyDaysAgo = today.AddDays(-29);
        var borrowChart = await _context.Transactions
            .Where(t => t.BorrowDate >= thirtyDaysAgo)
            .GroupBy(t => t.BorrowDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var returnChart = await _context.Transactions
            .Where(t => t.ReturnDate.HasValue && t.ReturnDate.Value.Date >= thirtyDaysAgo)
            .GroupBy(t => t.ReturnDate!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill in the date with no data
        var chartDates = Enumerable.Range(0, 30)
            .Select(i => thirtyDaysAgo.AddDays(i).Date)
            .ToList();

        var borrowChartFull = chartDates.Select(d => new
        {
            date  = d.ToString("dd/MM"),
            borrow = borrowChart.FirstOrDefault(x => x.Date == d)?.Count ?? 0,
            @return = returnChart.FirstOrDefault(x => x.Date == d)?.Count ?? 0
        }).ToList();

        // ---- Monthly borrowing chart (last 12 months) ----
        var twelveMonthsAgo = today.AddMonths(-11);
        var monthlyChart = await _context.Transactions
            .Where(t => t.BorrowDate >= twelveMonthsAgo)
            .GroupBy(t => new { t.BorrowDate.Year, t.BorrowDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        var months = Enumerable.Range(0, 12)
            .Select(i => twelveMonthsAgo.AddMonths(i))
            .ToList();

        var monthlyChartFull = months.Select(m => new
        {
            label = m.ToString("MM/yyyy"),
            count = monthlyChart.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month)?.Count ?? 0
        }).ToList();

        // ---- Top 10 most borrowed books ----
        var topBooks = await _context.Transactions
            .Include(t => t.Copy)
                .ThenInclude(c => c.Book)
            .Where(t => t.Copy != null && t.Copy.Book != null)
            .GroupBy(t => new { t.Copy!.Book!.BookId, t.Copy.Book.Title, t.Copy.Book.ImageUrl })
            .Select(g => new
            {
                g.Key.BookId,
                g.Key.Title,
                g.Key.ImageUrl,
                BorrowCount = g.Count()
            })
            .OrderByDescending(x => x.BorrowCount)
            .Take(10)
            .ToListAsync();

        // ---- Warning ----
        var overdueList = await _context.Transactions
            .Include(t => t.User)
                .ThenInclude(u => u.StudentProfile)
            .Include(t => t.Copy)
                .ThenInclude(c => c.Book)
            .Where(t => t.Status == TransactionStatus.Overdue)
            .OrderBy(t => t.DueDate)
            .Take(10)
            .Select(t => new
            {
                t.TransactionId,
                t.DueDate,
                UserName    = t.User != null ? t.User.FullName : null,
                StudentCode = t.User != null && t.User.StudentProfile != null
                    ? t.User.StudentProfile.StudentCode : null,
                BookTitle   = t.Copy != null && t.Copy.Book != null ? t.Copy.Book.Title : null,
                Barcode     = t.Copy != null ? t.Copy.Barcode : null
            })
            .ToListAsync();

        // Calculate OverdueDays after loading into memory
        var overdueListMapped = overdueList.Select(t => new
        {
            t.TransactionId,
            t.DueDate,
            OverdueDays = (int)(now - t.DueDate).TotalDays,
            t.UserName,
            t.StudentCode,
            t.BookTitle,
            t.Barcode
        }).ToList();

        var pendingFineList = await _context.Fines
            .Include(f => f.Transaction)
                .ThenInclude(t => t.User)
                    .ThenInclude(u => u.StudentProfile)
            .Include(f => f.Transaction)
                .ThenInclude(t => t.Copy)
                    .ThenInclude(c => c.Book)
            .Where(f => f.Status == FineStatus.Pending)
            .OrderByDescending(f => f.Amount)
            .Take(10)
            .Select(f => new
            {
                f.FineId,
                f.Amount,
                f.Reason,
                f.CreatedDate,
                UserName    = f.Transaction != null && f.Transaction.User != null
                    ? f.Transaction.User.FullName : null,
                StudentCode = f.Transaction != null && f.Transaction.User != null &&
                              f.Transaction.User.StudentProfile != null
                    ? f.Transaction.User.StudentProfile.StudentCode : null,
                BookTitle   = f.Transaction != null && f.Transaction.Copy != null &&
                              f.Transaction.Copy.Book != null
                    ? f.Transaction.Copy.Book.Title : null
            })
            .ToListAsync();
        
        var pendingRequestList = await _context.BorrowRequests
            .Include(r => r.User)
            .Include(r => r.Book)
            .Where(r => r.Status == RequestStatus.Pending)
            .OrderBy(r => r.ExpectedBorrowDate)
            .Take(10)
            .Select(r => new
            {
                r.RequestId,
                r.RequestDate,
                r.ExpectedBorrowDate,
                UserName = r.User != null ? r.User.FullName : null,
                BookTitle = r.Book != null ? r.Book.Title : null,
            })
            .ToListAsync();

        return Ok(new
        {
            // Overview
            overview = new
            {
                totalBooks,
                totalCopies,
                availableCopies,
                totalUsers,
                activeUsers,
                borrowing,
                overdue,
                pendingRequests,
                pendingFines,
                pendingFineAmount
            },
            // Charts
            charts = new
            {
                daily   = borrowChartFull,
                monthly = monthlyChartFull
            },
            // Top books
            topBooks,
            // Warning
            alerts = new
            {
                overdueList    = overdueListMapped,
                pendingFineList,
                pendingRequestList
            }
        });
    }
}