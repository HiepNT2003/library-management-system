using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookCopiesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BookCopiesController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/BookCopies?bookId=5
    [HttpGet]
    public async Task<IActionResult> GetByBookId([FromQuery] int bookId)
    {
        var copies = await _context.BookCopies
            .Include(c => c.Warehouse)
            .Where(c => c.BookId == bookId)
            .OrderBy(c => c.CopyId)
            .Select(c => new BookCopyDto
            {
                CopyId         = c.CopyId,
                BookId         = c.BookId,
                Barcode        = c.Barcode,
                Status         = c.Status.ToString(),
                ShelfLocation  = c.ShelfLocation,
                PurchaseDate   = c.PurchaseDate,
                BookCondition  = c.BookCondition,
                IsReferenceOnly = c.IsReferenceOnly,
                Notes          = c.Notes,
                WarehouseId    = c.WarehouseId,
                WarehouseName  = c.Warehouse != null ? c.Warehouse.Name : null
            })
            .ToListAsync();

        return Ok(copies);
    }

    // GET /api/BookCopies/all?warehouseId=1&status=Available&search=BC-001&page=1&pageSize=20
    [HttpGet("all")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? warehouseId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.BookCopies
            .Include(c => c.Warehouse)
            .Include(c => c.Book)
            .Where(c => !c.Book.IsDeleted)
            .AsQueryable();
 
        if (warehouseId.HasValue)
            query = query.Where(c => c.WarehouseId == warehouseId);
 
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookCopyStatus>(status, out var statusEnum))
            query = query.Where(c => c.Status == statusEnum);
 
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                (c.Barcode != null && c.Barcode.Contains(search)) ||
                (c.Book != null && c.Book.Title != null && c.Book.Title.Contains(search)));
 
        var total = await query.CountAsync();
 
        // Statistics by status (not affected by filter status)
        var statsQuery = _context.BookCopies.AsQueryable();
        if (warehouseId.HasValue) statsQuery = statsQuery.Where(c => c.WarehouseId == warehouseId);
        if (!string.IsNullOrWhiteSpace(search))
            statsQuery = statsQuery.Where(c =>
                (c.Barcode != null && c.Barcode.Contains(search)) ||
                (c.Book != null && c.Book.Title != null && c.Book.Title.Contains(search)));
 
        var stats = await statsQuery
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
 
        var items = await query
            .OrderBy(c => c.WarehouseId)
            .ThenBy(c => c.Barcode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new BookCopyAllDto
            {
                CopyId          = c.CopyId,
                BookId          = c.BookId,
                BookTitle       = c.Book != null ? c.Book.Title : null,
                Barcode         = c.Barcode,
                Status          = c.Status.ToString(),
                ShelfLocation   = c.ShelfLocation,
                BookCondition   = c.BookCondition,
                IsReferenceOnly = c.Book != null && c.Book.IsBorrowable.HasValue && !c.Book.IsBorrowable.Value ? true : c.IsReferenceOnly,
                WarehouseId     = c.WarehouseId,
                WarehouseName   = c.Warehouse != null ? c.Warehouse.Name : null,
                PurchaseDate    = c.PurchaseDate,
                Notes           = c.Notes
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
 
    // PATCH /api/BookCopies/5/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var copy = await _context.BookCopies.FindAsync(id);
        if (copy == null)
            return NotFound(new { message = $"Không tìm thấy bản sao với id {id}" });

        if (!Enum.TryParse<BookCopyStatus>(dto.Status, out var newStatus))
            return BadRequest(new { message = $"Trạng thái '{dto.Status}' không hợp lệ" });

        var allowed = new Dictionary<BookCopyStatus, List<BookCopyStatus>>
        {
            { BookCopyStatus.Available, new() { BookCopyStatus.Damaged, BookCopyStatus.Lost } },
            { BookCopyStatus.Borrowed,  new() { } },
            { BookCopyStatus.Damaged,   new() { BookCopyStatus.Available, BookCopyStatus.Lost } },
            { BookCopyStatus.Lost,      new() { } }
        };

        if (!allowed[copy.Status].Contains(newStatus))
            return BadRequest(new { message = $"Không thể chuyển từ '{copy.Status}' sang '{newStatus}'" });

        var changedById = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _context.BookCopyStatusHistories.Add(new BookCopyStatusHistory
        {
            CopyId      = id,
            OldStatus   = copy.Status.ToString(),
            NewStatus   = newStatus.ToString(),
            ChangedById = changedById,
            ChangedAt   = DateTime.Now,
            Reason      = dto.Reason
        });

        copy.Status = newStatus;
        await _context.SaveChangesAsync();

        return Ok(new { copyId = copy.CopyId, status = copy.Status.ToString() });
    }

    // GET /api/BookCopies/5/history
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(int id)
    {
        var copy = await _context.BookCopies.FindAsync(id);
        if (copy == null)
            return NotFound(new { message = $"Không tìm thấy bản sao với id {id}" });

        var history = await _context.BookCopyStatusHistories
            .Where(h => h.CopyId == id)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new
            {
                h.Id,
                h.OldStatus,
                h.NewStatus,
                h.ChangedAt,
                h.Reason,
                ChangedBy = h.ChangedBy != null ? (h.ChangedBy.FullName ?? h.ChangedBy.UserName): "Hệ thống"
            })
            .ToListAsync();

        return Ok(history);
    }

    // PUT /api/BookCopies/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateBookCopyDto dto)
    {
        var copy = await _context.BookCopies.FindAsync(id);
        if (copy == null)
            return NotFound(new { message = $"Không tìm thấy bản sao với id {id}" });

        if (!string.IsNullOrWhiteSpace(dto.Barcode) && dto.Barcode != copy.Barcode)
        {
            var barcodeExists = await _context.BookCopies.AnyAsync(c => c.Barcode == dto.Barcode && c.CopyId != id);
            if (barcodeExists)
                return Conflict(new { message = $"Barcode '{dto.Barcode}' đã tồn tại" });
        }

        if (dto.WarehouseId.HasValue)
        {
            var warehouseExists = await _context.Warehouses.AnyAsync(w => w.WarehouseId == dto.WarehouseId);
            if(!warehouseExists)
                return NotFound(new { message = $"Không tìm thấy phòng với id {dto.WarehouseId}" });
        }
 
        copy.Barcode         = dto.Barcode;
        copy.ShelfLocation   = dto.ShelfLocation;
        copy.BookCondition   = dto.BookCondition;
        copy.PurchaseDate    = dto.PurchaseDate;
        copy.IsReferenceOnly = dto.IsReferenceOnly;
        copy.Notes           = dto.Notes;
        copy.WarehouseId     = dto.WarehouseId ?? 1;
 
        await _context.SaveChangesAsync();
        await _context.Entry(copy).Reference(c => c.Warehouse).LoadAsync();
 
        return Ok(new BookCopyDto
        {
            CopyId          = copy.CopyId,
            BookId          = copy.BookId,
            Barcode         = copy.Barcode,
            Status          = copy.Status.ToString(),
            ShelfLocation   = copy.ShelfLocation,
            PurchaseDate    = copy.PurchaseDate,
            BookCondition   = copy.BookCondition,
            IsReferenceOnly = copy.IsReferenceOnly,
            Notes           = copy.Notes,
            WarehouseId     = copy.WarehouseId,
            WarehouseName   = copy.Warehouse?.Name
        });
    }
    
    // POST /api/BookCopies
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookCopyDto dto)
    {
        var book = await _context.Books
            .Where(b => b.BookId == dto.BookId && !b.IsDeleted)
            .Select(b => new { b.BookId, b.DocumentTypeId })
            .FirstOrDefaultAsync();

        if (book == null)
            return NotFound(new { message = $"Không tìm thấy sách với id {dto.BookId}" });

        if (book.DocumentTypeId != 1 && book.DocumentTypeId != 3)
            return BadRequest(new { message = "Chỉ sách vật lý và luận án mới có thể thêm bản sao vật lý" });
        
        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var barcodeExists = await _context.BookCopies.AnyAsync(c => c.Barcode == dto.Barcode);
            if (barcodeExists)
                return Conflict(new { message = $"Barcode '{dto.Barcode}' đã tồn tại" });
        }

        if(dto.WarehouseId.HasValue)
        {
            var warehouseExists = await _context.Warehouses.AnyAsync(w => w.WarehouseId == dto.WarehouseId);
            if(!warehouseExists)
                return NotFound(new { message = $"Không tìm thấy phòng với id {dto.WarehouseId}" });
        }

        if(!dto.WarehouseId.HasValue)
            return BadRequest("WarehouseId is required");
 
        var copy = new BookCopy
        {
            BookId          = dto.BookId,
            Barcode         = dto.Barcode,
            ShelfLocation   = dto.ShelfLocation,
            BookCondition   = dto.BookCondition,
            PurchaseDate    = dto.PurchaseDate,
            IsReferenceOnly = dto.IsReferenceOnly,
            Notes           = dto.Notes,
            WarehouseId     = dto.WarehouseId ?? 1,
            Status          = BookCopyStatus.Available
        };
 
        _context.BookCopies.Add(copy);
        await _context.SaveChangesAsync();
        await _context.Entry(copy).Reference(c => c.Warehouse).LoadAsync();
 
        return CreatedAtAction(nameof(GetByBookId), new { bookId = copy.BookId }, new BookCopyDto
        {
            CopyId          = copy.CopyId,
            BookId          = copy.BookId,
            Barcode         = copy.Barcode,
            Status          = copy.Status.ToString(),
            ShelfLocation   = copy.ShelfLocation,
            PurchaseDate    = copy.PurchaseDate,
            BookCondition   = copy.BookCondition,
            IsReferenceOnly = copy.IsReferenceOnly,
            Notes           = copy.Notes,
            WarehouseId     = copy.WarehouseId,
            WarehouseName   = copy.Warehouse?.Name
        });
    }
 
    // POST /api/BookCopies/bulk
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateBulk([FromBody] CreateBulkBookCopyDto dto)
    {
        if (dto.Quantity <= 0 || dto.Quantity > 100)
            return BadRequest(new { message = "Số lượng phải từ 1 đến 100" });
 
        var book = await _context.Books
            .Where(b => b.BookId == dto.BookId && !b.IsDeleted)
            .Select(b => new { b.BookId, b.DocumentTypeId })
            .FirstOrDefaultAsync();

        if (book == null)
            return NotFound(new { message = $"Không tìm thấy sách với id {dto.BookId}" });

        if (book.DocumentTypeId != 1 && book.DocumentTypeId != 3)
            return BadRequest(new { message = "Chỉ sách vật lý và luận án mới có thể thêm bản sao vật lý" });
 
        // Automatically generate barcode: take the current largest number and then increment
        var lastBarcode = await _context.BookCopies
            .Where(c => c.Barcode != null && c.Barcode.StartsWith("BC-"))
            .OrderByDescending(c => c.Barcode)
            .Select(c => c.Barcode)
            .FirstOrDefaultAsync();

        if(dto.WarehouseId.HasValue)
        {
            var warehouseExists = await _context.Warehouses.AnyAsync(w => w.WarehouseId == dto.WarehouseId);
            if(!warehouseExists)
                return NotFound(new { message = $"Không tìm thấy phòng với id {dto.WarehouseId}" });
        }

        if(!dto.WarehouseId.HasValue)
            return BadRequest("WarehouseId is required");
 
        var copies = Enumerable.Range(0, dto.Quantity).Select((_, i) =>
        {
            var now = DateTime.Now.AddMilliseconds(i); // cộng thêm i ms để tránh trùng trong cùng bulk
            var barcode = $"BC-{now:yyyyMMdd-HHmmss}{now.Millisecond:D3}";
            return new BookCopy
            {
                BookId          = dto.BookId,
                Barcode         = barcode,
                ShelfLocation   = dto.ShelfLocation,
                BookCondition   = dto.BookCondition,
                PurchaseDate    = dto.PurchaseDate,
                IsReferenceOnly = dto.IsReferenceOnly,
                WarehouseId     = dto.WarehouseId ?? 1,
                Status          = BookCopyStatus.Available
            };
        }).ToList();
 
        _context.BookCopies.AddRange(copies);
        await _context.SaveChangesAsync();

        var warehouseName = dto.WarehouseId.HasValue ? (await _context.Warehouses.FindAsync(dto.WarehouseId))?.Name : null;
 
        return Ok(copies.Select(c => new BookCopyDto
        {
            CopyId          = c.CopyId,
            BookId          = c.BookId,
            Barcode         = c.Barcode,
            Status          = c.Status.ToString(),
            ShelfLocation   = c.ShelfLocation,
            PurchaseDate    = c.PurchaseDate,
            BookCondition   = c.BookCondition,
            IsReferenceOnly = c.IsReferenceOnly,
            Notes           = c.Notes,
            WarehouseId     = c.WarehouseId,
            WarehouseName   = warehouseName
        }));
    }

    // DELETE /api/BookCopies/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var copy = await _context.BookCopies.FindAsync(id);
        if (copy == null)
            return NotFound(new { message = $"Không tìm thấy bản sao với id {id}" });

        if (copy.Status == BookCopyStatus.Borrowed)
            return BadRequest(new { message = "Không thể xoá bản sao đang được mượn" });

        var hasTransaction = await _context.Transactions.AnyAsync(t => t.CopyId == id);
        if (hasTransaction)
            return BadRequest(new { message = "Không thể xoá bản sao đã có lịch sử mượn trả" });

        _context.BookCopies.Remove(copy);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/BookCopies/import
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] List<ImportBookCopyDto> dtos)
    {
        if (dtos == null || dtos.Count == 0)
            return BadRequest(new { message = "Danh sách trống" });

        // Validate that bookIds exist and are of the correct document type
        var bookIds = dtos.Select(d => d.BookId).Distinct().ToList();
        var existingBooks = await _context.Books
            .Where(b => bookIds.Contains(b.BookId) && !b.IsDeleted)
            .Select(b => new { b.BookId, b.DocumentTypeId })
            .ToListAsync();

        var existingBookIds = existingBooks.Select(b => b.BookId).ToList();
        var invalidBookIds = bookIds.Except(existingBookIds).ToList();
        if (invalidBookIds.Any())
            return BadRequest(new { message = $"BookId không tồn tại: {string.Join(", ", invalidBookIds)}" });

        // Only the physics book (1) and dissertations (3) have physical copies
        var invalidTypeBookIds = existingBooks
            .Where(b => b.DocumentTypeId != 1 && b.DocumentTypeId != 3)
            .Select(b => b.BookId)
            .ToList();
        if (invalidTypeBookIds.Any())
            return BadRequest(new { message = $"Các BookId sau không phải sách vật lý hoặc luận án: {string.Join(", ", invalidTypeBookIds)}" });

        // Validate warehouseIds tồn tại
        var warehouseIds = dtos.Select(d => d.WarehouseId).Distinct().ToList();
        var existingWarehouseIds = await _context.Warehouses
            .Where(w => warehouseIds.Contains(w.WarehouseId))
            .Select(w => w.WarehouseId)
            .ToListAsync();

        var invalidWarehouseIds = warehouseIds.Except(existingWarehouseIds).ToList();
        if (invalidWarehouseIds.Any())
            return BadRequest(new { message = $"WarehouseId không tồn tại: {string.Join(", ", invalidWarehouseIds)}" });

        // Validate barcode trùng với DB
        var barcodes = dtos
            .Where(d => !string.IsNullOrWhiteSpace(d.Barcode))
            .Select(d => d.Barcode)
            .ToList();

        if (barcodes.Any())
        {
            var duplicateBarcodes = await _context.BookCopies
                .Where(c => barcodes.Contains(c.Barcode))
                .Select(c => c.Barcode)
                .ToListAsync();

            if (duplicateBarcodes.Any())
                return BadRequest(new { message = $"Barcode đã tồn tại trong hệ thống: {string.Join(", ", duplicateBarcodes)}" });
        }

        // Create BookCopy
        var copies = dtos.Select((dto, i) =>
        {
            var barcode = !string.IsNullOrWhiteSpace(dto.Barcode)
                ? dto.Barcode
                : $"BC-{DateTime.Now.AddMilliseconds(i):yyyyMMdd-HHmmss}{DateTime.Now.AddMilliseconds(i).Millisecond:D3}";

            return new BookCopy
            {
                BookId          = dto.BookId,
                Barcode         = barcode,
                WarehouseId     = dto.WarehouseId,
                ShelfLocation   = dto.ShelfLocation,
                BookCondition   = dto.BookCondition,
                PurchaseDate    = dto.PurchaseDate,
                IsReferenceOnly = dto.IsReferenceOnly,
                Notes           = dto.Notes,
                Status          = BookCopyStatus.Available
            };
        }).ToList();

        _context.BookCopies.AddRange(copies);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = copies.Count,
            message = $"Import thành công {copies.Count} bản sao"
        });
    }
}
