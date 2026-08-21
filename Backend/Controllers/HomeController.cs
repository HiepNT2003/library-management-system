using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/BooksSearch")]
public class HomeController : ControllerBase
{
    private readonly AppDbContext _context;

    public HomeController (AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("total")]
    public async Task<IActionResult> GetDiscover()
    {
        var totalBooks       = await _context.Books.CountAsync(b => !b.IsDeleted);
        var totalCopies      = await _context.BookCopies.CountAsync();
        var availableCopies  = await _context.BookCopies.CountAsync(c => c.Status == BookCopyStatus.Available);
        return Ok(new
        {

            totalBooks,
            totalCopies,
            availableCopies,
        });
    }

    // GET /api/BooksSearch?keyword=&page=1&pageSize=20&sort=newest
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? keyword,
        [FromQuery] int? documentTypeId,
        [FromQuery] int? categoryId,
        [FromQuery] int? languageId,
        [FromQuery] int? authorId,
        [FromQuery] string? ddcCode,
        [FromQuery] int? fromYear,
        [FromQuery] int? toYear,
        [FromQuery] string sort = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Books
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookCategories).ThenInclude(bc => bc.Category)
            .Include(b => b.BookLanguages).ThenInclude(bl => bl.Language)
            .Where(b => !b.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(b =>
                b.Title.Contains(keyword) ||
                b.BookAuthors.Any(ba => ba.Author.Name.Contains(keyword)) ||
                (b.Publisher != null && b.Publisher.Contains(keyword)) ||
                (b.DDCCode != null && b.DDCCode.Contains(keyword)) ||
                (b.ISBN != null && b.ISBN.Contains(keyword)));

        if (documentTypeId.HasValue)
            query = query.Where(b => b.DocumentTypeId == documentTypeId);

        if (categoryId.HasValue)
            query = query.Where(b => b.BookCategories.Any(bc => bc.CategoryId == categoryId));

        if (languageId.HasValue)
            query = query.Where(b => b.BookLanguages.Any(bl => bl.LanguageId == languageId));

        if (authorId.HasValue)
            query = query.Where(b => b.BookAuthors.Any(ba => ba.AuthorId == authorId));

        if (!string.IsNullOrWhiteSpace(ddcCode))
            query = query.Where(b => b.DDCCode != null && b.DDCCode.StartsWith(ddcCode));

        if (fromYear.HasValue)
            query = query.Where(b => b.PublishedYear >= fromYear);

        if (toYear.HasValue)
            query = query.Where(b => b.PublishedYear <= toYear);

        // Sort
        query = sort switch
        {
            "title"      => query.OrderBy(b => b.Title),
            "oldest"     => query.OrderBy(b => b.PublishedYear),
            "popular"    => query.OrderByDescending(b => b.BookCopies
                                .Count(c => c.Transactions.Any())),
            _            => query.OrderByDescending(b => b.CreatedDate)
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.BookId,
                b.Title,
                b.ImageUrl,
                b.Publisher,
                b.PublishedYear,
                b.DocumentTypeId,
                b.DDCCode,
                Authors    = b.BookAuthors.Select(ba => new { ba.Author.AuthorId, ba.Author.Name }),
                Categories = b.BookCategories.Select(bc => new { bc.Category.CategoryId, bc.Category.Name }),
                Languages  = b.BookLanguages.Select(bl => new { bl.Language.LanguageId, bl.Language.Name }),
                AvailableCopies = b.BookCopies.Count(c => c.Status == BookCopyStatus.Available && !c.IsReferenceOnly),
                TotalCopies     = b.BookCopies.Count()
            })
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

    // POST /api/BooksSearch/advanced-search
    [HttpPost("advanced-search")]
    public async Task<IActionResult> AdvancedSearch(
        [FromBody] AdvancedSearchDto dto,
        [FromQuery] string sort = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Books
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookCategories).ThenInclude(bc => bc.Category)
            .Include(b => b.BookLanguages).ThenInclude(bl => bl.Language)
            .Where(b => !b.IsDeleted)
            .AsQueryable();

        if (dto.Conditions != null && dto.Conditions.Count > 0)
        {
            // The first condition is always ANDed with the original query
            var first = dto.Conditions[0];
            query = ApplyCondition(query, first.Field, first.Value);

            // The next conditions
            for (int i = 1; i < dto.Conditions.Count; i++)
            {
                var cond = dto.Conditions[i];
                if (string.IsNullOrWhiteSpace(cond.Value)) continue;

                switch (cond.Operator?.ToUpper())
                {
                    case "OR":
                        // OR: union with the current query
                        var orQuery = ApplyCondition(
                            _context.Books
                                .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
                                .Include(b => b.BookCategories).ThenInclude(bc => bc.Category)
                                .Include(b => b.BookLanguages).ThenInclude(bl => bl.Language)
                                .Where(b => !b.IsDeleted),
                            cond.Field, cond.Value);
                        query = query.Union(orQuery);
                        break;

                    case "NOT":
                        // NOTE: exclude the bookIds that meet the condition
                        var notIds = await ApplyCondition(
                            _context.Books.Where(b => !b.IsDeleted),
                            cond.Field, cond.Value)
                            .Select(b => b.BookId)
                            .ToListAsync();
                        query = query.Where(b => !notIds.Contains(b.BookId));
                        break;

                    default: // AND
                        query = ApplyCondition(query, cond.Field, cond.Value);
                        break;
                }
            }
        }

        // Sort
        query = sort switch
        {
            "title"   => query.OrderBy(b => b.Title),
            "oldest"  => query.OrderBy(b => b.PublishedYear),
            "popular" => query.OrderByDescending(b => b.BookCopies.Count(c => c.Transactions.Any())),
            _         => query.OrderByDescending(b => b.CreatedDate)
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.BookId, b.Title, b.ImageUrl, b.Publisher, b.PublishedYear, b.DocumentTypeId,
                Authors    = b.BookAuthors.Select(ba => new { ba.Author.AuthorId, ba.Author.Name }),
                Categories = b.BookCategories.Select(bc => new { bc.Category.CategoryId, bc.Category.Name }),
                AvailableCopies = b.BookCopies.Count(c => c.Status == BookCopyStatus.Available && !c.IsReferenceOnly),
                TotalCopies     = b.BookCopies.Count()
            })
            .ToListAsync();

        return Ok(new
        {
            items, total, page, pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    // GET /api/Books/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var book = await _context.Books
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookCategories).ThenInclude(bc => bc.Category)
            .Include(b => b.BookLanguages).ThenInclude(bl => bl.Language)
            .Include(b => b.BookCopies).ThenInclude(c => c.Warehouse)
            .FirstOrDefaultAsync(b => b.BookId == id && !b.IsDeleted);

        if (book == null)
            return NotFound(new { message = "Không tìm thấy sách" });

        // Record reading history if logged in
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var history = await _context.UserReadingHistories
                .FirstOrDefaultAsync(h => h.UserId == userId && h.BookId == id);

            if (history == null)
            {
                _context.UserReadingHistories.Add(new UserReadingHistory
                {
                    UserId    = userId,
                    BookId    = id,
                    CreatedAt  = DateTime.Now
                });
            }
            else
            {
                history.CreatedAt = DateTime.Now;
            }
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            book.BookId, book.Title, book.Description, book.ImageUrl,
            book.Publisher, book.PublishedYear, book.DocumentTypeId,
            book.DDCCode, book.ISBN, book.TotalPages, book.IsBorrowable,
            // Ebook fields
            book.FilePath, book.IsPublic,
            // Thesis fields
            book.University, book.Faculty, book.Degree, book.DefenseYear,
            // Article fields
            book.Source, book.StartPage, book.EndPage,
            Authors    = book.BookAuthors.Select(ba => new { ba.Author.AuthorId, ba.Author.Name, ba.Author.Bio, ba.Author.ImageUrl }),
            Categories = book.BookCategories.Select(bc => new { bc.Category.CategoryId, bc.Category.Name }),
            Languages  = book.BookLanguages.Select(bl => new { bl.Language.LanguageId, bl.Language.Name, bl.Language.Code }),
            Copies = book.BookCopies.Select(c => new
            {
                c.CopyId, c.Barcode, c.Status, c.ShelfLocation, c.IsReferenceOnly,
                WarehouseName = c.Warehouse != null ? c.Warehouse.Name : null
            }),
            AvailableCopies = book.BookCopies.Count(c => c.Status == BookCopyStatus.Available && !c.IsReferenceOnly),
            TotalCopies     = book.BookCopies.Count()
        });
    }

    // ---- Helper ----
    private IQueryable<Book> ApplyCondition(IQueryable<Book> query, string? field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return query;

        return field?.ToLower() switch
        {
            "title"     => query.Where(b => b.Title.Contains(value)),
            "author"    => query.Where(b => b.BookAuthors.Any(ba => ba.Author.Name.Contains(value))),
            "category"  => query.Where(b => b.BookCategories.Any(bc => bc.Category.Name.Contains(value))),
            "publisher" => query.Where(b => b.Publisher != null && b.Publisher.Contains(value)),
            "ddc"       => query.Where(b => b.DDCCode != null && b.DDCCode.StartsWith(value)),
            "year"      => int.TryParse(value, out var y) ? query.Where(b => b.PublishedYear == y) : query,
            "language"  => query.Where(b => b.BookLanguages.Any(bl => bl.Language.Name.Contains(value) || bl.Language.Code == value)),
            "isbn"      => query.Where(b => b.ISBN != null && b.ISBN.Contains(value)),
            _           => query.Where(b => // "all"
                b.Title.Contains(value) ||
                b.BookAuthors.Any(ba => ba.Author.Name.Contains(value)) ||
                (b.Publisher != null && b.Publisher.Contains(value)) ||
                (b.DDCCode != null && b.DDCCode.Contains(value)))
        };
    }
}

// DTOs
public class AdvancedSearchDto
{
    public List<SearchConditionDto> Conditions { get; set; } = new();
}

public class SearchConditionDto
{
    public string? Operator { get; set; }  // AND / OR / NOT (null for the first condition)
    public string? Field { get; set; }     // all / title / author / category / publisher / ddc / year / language / isbn
    public string? Value { get; set; }
}