using Backend.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/Reports")]
[Authorize(Roles = "Admin,Librarian")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/Reports/summary?from=2024-01-01&to=2024-12-31
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.Today.AddMonths(-1);
        var toDate   = to   ?? DateTime.Today;
        toDate       = toDate.Date.AddDays(1).AddSeconds(-1); // end of the day

        // Borrowing and returning statistics
        var transactionsRaw = await _context.Transactions
            .Where(t => t.BorrowDate >= fromDate && t.BorrowDate <= toDate)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var transactions = transactionsRaw
            .Select(g => new { Status = g.Status.ToString(), g.Count })
            .ToList();

        // Penalty stats
        var finesRaw = await _context.Fines
            .Where(f => f.CreatedDate >= fromDate && f.CreatedDate <= toDate)
            .GroupBy(f => f.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(f => f.Amount) })
            .ToListAsync();

        var fines = finesRaw
            .Select(g => new { Status = g.Status.ToString(), g.Count, g.Total })
            .ToList();

        // Borrowing request statistics
        var requestsRaw = await _context.BorrowRequests
            .Where(r => r.RequestDate >= fromDate && r.RequestDate <= toDate)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var requests = requestsRaw
            .Select(g => new { Status = g.Status.ToString(), g.Count })
            .ToList();

        // Top 10 most borrowed books of the term
        var topBooks = await _context.Transactions
            .Include(t => t.Copy).ThenInclude(c => c.Book)
            .Where(t => t.BorrowDate >= fromDate &&
                        t.BorrowDate <= toDate &&
                        t.Copy != null && t.Copy.Book != null)
            .GroupBy(t => new { t.Copy!.Book!.BookId, t.Copy.Book.Title })
            .Select(g => new
            {
                g.Key.BookId,
                g.Key.Title,
                BorrowCount = g.Count()
            })
            .OrderByDescending(x => x.BorrowCount)
            .Take(10)
            .ToListAsync();

        // Borrow and pay back monthly
        var monthlyBorrow = await _context.Transactions
            .Where(t => t.BorrowDate >= fromDate && t.BorrowDate <= toDate)
            .GroupBy(t => new { t.BorrowDate.Year, t.BorrowDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        return Ok(new
        {
            period = new { from = fromDate, to = toDate },
            transactions,
            fines,
            requests,
            topBooks,
            monthlyBorrow
        });
    }

    // GET /api/Reports/transactions/export?from=&to=&status=
    [HttpGet("transactions/export")]
    public async Task<IActionResult> ExportTransactions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status)
    {
        var fromDate = from ?? DateTime.Today.AddMonths(-1);
        var toDate   = (to ?? DateTime.Today).Date.AddDays(1).AddSeconds(-1);

        var query = _context.Transactions
            .Include(t => t.User).ThenInclude(u => u.StudentProfile)
            .Include(t => t.Copy).ThenInclude(c => c.Book)
            .Where(t => t.BorrowDate >= fromDate && t.BorrowDate <= toDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TransactionStatus>(status, out var statusEnum))
            query = query.Where(t => t.Status == statusEnum);

        var items = await query
            .OrderByDescending(t => t.BorrowDate)
            .ToListAsync();

        using var workbook  = new XLWorkbook();
        var worksheet       = workbook.Worksheets.Add("Giao dịch mượn trả");

        // Header
        var headers = new[]
        {
            "STT", "Mã GD", "Họ tên", "Mã SV", "Tên sách", "Barcode",
            "Ngày mượn", "Hạn trả", "Ngày trả", "Trạng thái", "Ghi chú"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value                          = headers[i];
            cell.Style.Font.Bold                = true;
            cell.Style.Fill.BackgroundColor     = XLColor.FromHtml("#3949AB");
            cell.Style.Font.FontColor           = XLColor.White;
            cell.Style.Alignment.Horizontal     = XLAlignmentHorizontalValues.Center;
        }

        // Data
        for (int i = 0; i < items.Count; i++)
        {
            var t   = items[i];
            var row = i + 2;
            worksheet.Cell(row, 1).Value  = i + 1;
            worksheet.Cell(row, 2).Value  = t.TransactionId;
            worksheet.Cell(row, 3).Value  = t.User?.FullName ?? "";
            worksheet.Cell(row, 4).Value  = t.User?.StudentProfile?.StudentCode ?? "";
            worksheet.Cell(row, 5).Value  = t.Copy?.Book?.Title ?? "";
            worksheet.Cell(row, 6).Value  = t.Copy?.Barcode ?? "";
            worksheet.Cell(row, 7).Value  = t.BorrowDate.ToString("dd/MM/yyyy");
            worksheet.Cell(row, 8).Value  = t.DueDate.ToString("dd/MM/yyyy");
            worksheet.Cell(row, 9).Value  = t.ReturnDate?.ToString("dd/MM/yyyy") ?? "";
            worksheet.Cell(row, 10).Value = t.Status.ToString();
            worksheet.Cell(row, 11).Value = t.Notes ?? "";

            // Highlight over due
            if (t.Status == TransactionStatus.Overdue)
                worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
        }

        worksheet.Columns().AdjustToContents();
        worksheet.Row(1).Height = 20;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);

        var fileName = $"GiaoDich_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    // GET /api/Reports/fines/export?from=&to=&status=
    [HttpGet("fines/export")]
    public async Task<IActionResult> ExportFines(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? status)
    {
        var fromDate = from ?? DateTime.Today.AddMonths(-1);
        var toDate   = (to ?? DateTime.Today).Date.AddDays(1).AddSeconds(-1);

        var query = _context.Fines
            .Include(f => f.Transaction).ThenInclude(t => t.User)
                .ThenInclude(u => u.StudentProfile)
            .Include(f => f.Transaction).ThenInclude(t => t.Copy)
                .ThenInclude(c => c.Book)
            .Where(f => f.CreatedDate >= fromDate && f.CreatedDate <= toDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<FineStatus>(status, out var statusEnum))
            query = query.Where(f => f.Status == statusEnum);

        var items = await query.OrderByDescending(f => f.CreatedDate).ToListAsync();

        using var workbook  = new XLWorkbook();
        var worksheet       = workbook.Worksheets.Add("Phiếu phạt");

        var headers = new[]
        {
            "STT", "Mã phạt", "Họ tên", "Mã SV", "Tên sách",
            "Lý do", "Số tiền", "Trạng thái", "Ngày tạo", "Ngày thu", "Ghi chú"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value                      = headers[i];
            cell.Style.Font.Bold            = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#C62828");
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (int i = 0; i < items.Count; i++)
        {
            var f   = items[i];
            var row = i + 2;
            worksheet.Cell(row, 1).Value  = i + 1;
            worksheet.Cell(row, 2).Value  = f.FineId;
            worksheet.Cell(row, 3).Value  = f.Transaction?.User?.FullName ?? "";
            worksheet.Cell(row, 4).Value  = f.Transaction?.User?.StudentProfile?.StudentCode ?? "";
            worksheet.Cell(row, 5).Value  = f.Transaction?.Copy?.Book?.Title ?? "";
            worksheet.Cell(row, 6).Value  = f.Reason;
            worksheet.Cell(row, 7).Value  = (double)f.Amount;
            worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, 8).Value  = f.Status.ToString();
            worksheet.Cell(row, 9).Value = f.CreatedDate.HasValue
            ? f.CreatedDate.Value.ToString("dd/MM/yyyy")
            : "";
            worksheet.Cell(row, 10).Value = f.PaidDate?.ToString("dd/MM/yyyy") ?? "";
            worksheet.Cell(row, 11).Value = f.Note ?? "";

            if (f.Status == FineStatus.Pending)
                worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF8E1");
        }

        // Total amount
        var totalRow = items.Count + 2;
        worksheet.Cell(totalRow, 6).Value      = "Tổng cộng:";
        worksheet.Cell(totalRow, 6).Style.Font.Bold = true;
        worksheet.Cell(totalRow, 7).Value      = (double)items.Sum(f => f.Amount);
        worksheet.Cell(totalRow, 7).Style.Font.Bold = true;
        worksheet.Cell(totalRow, 7).Style.NumberFormat.Format = "#,##0";

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"PhieuPhat_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    // GET /api/Reports/books/export — copys' status
    [HttpGet("books/export")]
    public async Task<IActionResult> ExportBooks()
    {
        var books = await _context.Books
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookCopies)
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.Title)
            .ToListAsync();

        using var workbook  = new XLWorkbook();
        var worksheet       = workbook.Worksheets.Add("Danh sách sách");

        var headers = new[]
        {
            "STT", "Tên sách", "Tác giả", "ISBN", "NXB", "Năm XB",
            "Tổng bản sao", "Đang mượn", "Khả dụng", "Hư hỏng", "Mất"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value                      = headers[i];
            cell.Style.Font.Bold            = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        for (int i = 0; i < books.Count; i++)
        {
            var b   = books[i];
            var row = i + 2;
            worksheet.Cell(row, 1).Value  = i + 1;
            worksheet.Cell(row, 2).Value  = b.Title;
            worksheet.Cell(row, 3).Value  = string.Join(", ", b.BookAuthors.Select(ba => ba.Author.Name));
            worksheet.Cell(row, 4).Value  = b.ISBN ?? "";
            worksheet.Cell(row, 5).Value  = b.Publisher ?? "";
            worksheet.Cell(row, 6).Value  = b.PublishedYear?.ToString() ?? "";
            worksheet.Cell(row, 7).Value  = b.BookCopies.Count;
            worksheet.Cell(row, 8).Value  = b.BookCopies.Count(c => c.Status == BookCopyStatus.Borrowed);
            worksheet.Cell(row, 9).Value  = b.BookCopies.Count(c => c.Status == BookCopyStatus.Available);
            worksheet.Cell(row, 10).Value = b.BookCopies.Count(c => c.Status == BookCopyStatus.Damaged);
            worksheet.Cell(row, 11).Value = b.BookCopies.Count(c => c.Status == BookCopyStatus.Lost);
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"DanhSachSach_{DateTime.Today:yyyyMMdd}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}