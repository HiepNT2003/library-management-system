using Backend.DTOs.Books;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly AppDbContext _context;
    private Book CreateBaseBook(BaseCreateBookDto dto, int type)
    {
        return new Book
        {
            Title = dto.Title.Trim(),
            Description = dto.Description,
            DocumentTypeId = type,
            DDCCode = dto.DDCCode,
            Publisher = dto.Publisher,
            PublishedYear = dto.PublishedYear,
            ImageUrl = dto.ImageUrl,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };
    }
    private async Task AddRelations(Book book, BaseCreateBookDto dto)
    {
        if (dto.AuthorIds?.Any() == true)
        {
            await _context.BookAuthors.AddRangeAsync(
                dto.AuthorIds.Distinct().Select(id => new BookAuthor
                {
                    BookId = book.BookId,
                    AuthorId = id
                }));
        }

        if (dto.CategoryIds?.Any() == true)
        {
            await _context.BookCategories.AddRangeAsync(
                dto.CategoryIds.Distinct().Select(id => new BookCategory
                {
                    BookId = book.BookId,
                    CategoryId = id
                }));
        }

        if (dto.LanguageIds?.Any() == true)
        {
            await _context.BookLanguages.AddRangeAsync(
                dto.LanguageIds.Distinct().Select(id => new BookLanguage
                {
                    BookId = book.BookId,
                    LanguageId = id
                }));
        }
    }

    public BooksController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetBooks(
        string? search,
        int? categoryId,
        int? documentTypeId,
        string? sortBy,
        string? sortOrder = "asc",
        int page = 1,
        int pageSize = 12)
    {
        // 1. Base query
        var query = _context.Books
            .Include(b => b.BookCategories)
                .ThenInclude(bc => bc.Category)
            .Include(b => b.BookAuthors)
                .ThenInclude(ba => ba.Author)
            .AsQueryable();

        // 2. SEARCH
        if (!string.IsNullOrEmpty(search))
        {
            var keyword = search.Trim();

            if (keyword.All(char.IsDigit))
            {
                // search by ISBN
                query = query.Where(b => b.ISBN != null && b.ISBN.Contains(keyword));
            }
            else
            {
                // search by title + ISBN
                query = query.Where(b =>
                    b.Title.Contains(keyword) ||
                    (b.ISBN != null && b.ISBN.Contains(keyword))
                );
            }
        }

        // 3. FILTER
        if (categoryId.HasValue)
        {
            query = query.Where(b => b.BookCategories
                .Any(bc => bc.CategoryId == categoryId));
        }
        if (documentTypeId.HasValue)
        {
            query = query.Where(b => b.DocumentTypeId == documentTypeId);
        }

        // 4. SORTING
        bool isAsc = sortOrder?.ToLower() != "desc";

        query = sortBy?.ToLower() switch
        {
            "title" => isAsc ? query.OrderBy(b => b.Title) : query.OrderByDescending(b => b.Title),

            "publishedyear" => isAsc ? query.OrderBy(b => b.PublishedYear) : query.OrderByDescending(b => b.PublishedYear),

            "totalcopies" => isAsc
                ? query.OrderBy(b => b.BookCopies.Count())
                : query.OrderByDescending(b => b.BookCopies.Count()),

            "availablecopies" => isAsc
                ? query.OrderBy(b => b.BookCopies.Count(c => c.Status == BookCopyStatus.Available))
                : query.OrderByDescending(b => b.BookCopies.Count(c => c.Status == BookCopyStatus.Available)),

            "price" => isAsc ? query.OrderBy(b => b.Price) : query.OrderByDescending(b => b.Price),

            "author" => isAsc
                ? query.OrderBy(b => b.BookAuthors
                    .Select(ba => ba.Author.Name)
                    .FirstOrDefault())
                : query.OrderByDescending(b => b.BookAuthors
                    .Select(ba => ba.Author.Name)
                    .FirstOrDefault()),
            _ => query.OrderBy(b => b.BookId)
        };

        // 5. TOTAL COUNT
        var totalRecords = await query.CountAsync();

        // 6. PAGINATION + SELECT
        var books = await query
            .Where(b => !b.IsDeleted)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.BookId,
                b.Title,
                b.Publisher,
                b.PublishedYear,
                b.ImageUrl,
                ddc = b.DDC == null ? null : new
                {
                    b.DDC.Code,
                    b.DDC.Name
                },

                authors = b.BookAuthors.Select(ba => new
                {
                    ba.Author.AuthorId,
                    ba.Author.Name
                }),

                categories = b.BookCategories.Select(bc => new
                {
                    bc.Category.CategoryId,
                    bc.Category.Name
                }),

                languages = b.BookLanguages.Select(bl => new
                {
                    bl.Language.LanguageId,
                    bl.Language.Name
                }),
                b.ISBN,
                b.TotalPages,
                b.Price,
                b.IsBorrowable,
                b.Location,
                TotalCopies = b.BookCopies.Count(),
                AvailableCopies = b.BookCopies.Count(c => c.Status == BookCopyStatus.Available),
                b.Source,
                b.University,
                b.Faculty,
                b.DefenseYear,
                b.FileSize,
                b.IsPublic
            })
            .ToListAsync();

        // 7. RESPONSE
        var result = new
        {
            data = books,
            meta = new
            {
                page,
                pageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling((double)totalRecords / pageSize)
            }
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBookById(int id)
    {
        var b = await _context.Books
            .Include(x => x.DocumentType)
            .Include(x => x.BookAuthors).ThenInclude(x => x.Author)
            .Include(x => x.BookCategories).ThenInclude(x => x.Category)
            .Include(x => x.BookLanguages).ThenInclude(x => x.Language)
            .Include(x => x.DDC)
            .FirstOrDefaultAsync(x => x.BookId == id);

        if (b == null)
            return NotFound(new { message = "Book not found" });

        var baseDto = new
        {
            b.BookId,
            b.Title,
            b.Description,
            b.DocumentTypeId,
            ddc = b.DDC == null ? null : new
            {
                b.DDC.Code,
                b.DDC.Name
            },
            b.Publisher,
            b.PublishedYear,
            b.ImageUrl,
            authors = b.BookAuthors.Select(ba => ba.Author.AuthorId),
            categories = b.BookCategories.Select(bc => bc.Category.CategoryId),
            languages = b.BookLanguages.Select(bl => bl.Language.LanguageId)
        };
        switch (b.DocumentTypeId)
        {
            case 1:
                var physical = new
                {
                    baseDto.BookId,
                    baseDto.Title,
                    baseDto.Description,
                    baseDto.DocumentTypeId,
                    baseDto.ddc,
                    baseDto.Publisher,
                    baseDto.PublishedYear,
                    baseDto.ImageUrl,
                    baseDto.authors,
                    baseDto.categories,
                    baseDto.languages,
                    ISBN = b.ISBN!,
                    b.TotalPages,
                    b.Price,
                    b.IsBorrowable,
                    b.Location,
                    FilePath = b.FilePath!,
                    b.FileSize,
                    b.IsPublic
                };
                return Ok(physical);

            case 2:
                var article = new 
                {
                    baseDto.BookId,
                    baseDto.Title,
                    baseDto.Description,
                    baseDto.DocumentTypeId,
                    baseDto.ddc,
                    baseDto.Publisher,
                    baseDto.PublishedYear,
                    baseDto.ImageUrl,
                    baseDto.authors,
                    baseDto.categories,
                    baseDto.languages,
                    Source = b.Source!,
                    b.StartPage,
                    b.EndPage
                };
                return Ok(article);

            case 3:
                var thesis = new 
                {
                    baseDto.BookId,
                    baseDto.Title,
                    baseDto.Description,
                    baseDto.DocumentTypeId,
                    baseDto.ddc,
                    baseDto.Publisher,
                    baseDto.PublishedYear,
                    baseDto.ImageUrl,
                    baseDto.authors,
                    baseDto.categories,
                    baseDto.languages,
                    FilePath = b.FilePath!,
                    b.FileSize,
                    b.IsPublic,

                    University = b.University!,
                    b.Faculty,
                    b.Advisor,
                    b.Degree,
                    b.DefenseYear
                };
                return Ok(thesis);

            case 4:
                var ebook = new 
                {
                    baseDto.BookId,
                    baseDto.Title,
                    baseDto.Description,
                    baseDto.DocumentTypeId,
                    baseDto.ddc,
                    baseDto.Publisher,
                    baseDto.PublishedYear,
                    baseDto.ImageUrl,
                    baseDto.authors,
                    baseDto.categories,
                    baseDto.languages,

                    FilePath = b.FilePath!,
                    b.FileSize,
                    b.IsPublic
                };
                return Ok(ebook);

            default:
                return Ok(baseDto);
        }
    }

    [HttpGet("user/{id}")]
    public async Task<IActionResult> GetUserBookById(int id)
    {
        var b = await _context.Books
            .Include(x => x.DocumentType)
            .Include(x => x.BookAuthors).ThenInclude(x => x.Author)
            .Include(x => x.BookCategories).ThenInclude(x => x.Category)
            .Include(x => x.BookLanguages).ThenInclude(x => x.Language)
            .Include(x => x.DDC)
            .FirstOrDefaultAsync(x => x.BookId == id);

        if (b == null)
            return NotFound(new { message = "Book not found" });

        var baseDto = new
        {
            b.BookId,
            b.Title,
            b.Description,
            b.DocumentTypeId,
            ddc = b.DDC == null ? null : new
            {
                b.DDC.Code,
                b.DDC.Name
            },
            b.Publisher,
            b.PublishedYear,
            b.ImageUrl,
            authors = b.BookAuthors.Select(ba => new { ba.Author.AuthorId, ba.Author.Name }),
            categories = b.BookCategories.Select(bc => new { bc.Category.CategoryId, bc.Category.Name }),
            languages = b.BookLanguages.Select(bl => new { bl.Language.LanguageId, bl.Language.Name })
        };
        var totalCopies = await _context.BookCopies
            .CountAsync(c => c.BookId == id);

        var availableCopies = await _context.BookCopies
            .CountAsync(c => c.BookId == id && c.Status == BookCopyStatus.Available);
        switch (b.DocumentTypeId)
        {
            case 1:
                var physical = new
                {
                    baseDto.BookId,
                    baseDto.Title,
                    baseDto.Description,
                    baseDto.DocumentTypeId,
                    baseDto.ddc,
                    baseDto.Publisher,
                    baseDto.PublishedYear,
                    baseDto.ImageUrl,
                    baseDto.authors,
                    baseDto.categories,
                    baseDto.languages,
                    FilePath = b.FilePath!,
                    b.FileSize,
                    b.IsPublic,
                    b.DownloadCount,
                    
                    ISBN = b.ISBN!,
                    totalCopies,
                    availableCopies,
                    b.TotalPages,
                    b.Price,
                    b.IsBorrowable,
                    b.Location
                };
                return Ok(physical);

            case 2:
                var article = new 
                {
                    baseDto.BookId,
                    baseDto.Title,
                    baseDto.Description,
                    baseDto.DocumentTypeId,
                    baseDto.ddc,
                    baseDto.Publisher,
                    baseDto.PublishedYear,
                    baseDto.ImageUrl,
                    baseDto.authors,
                    baseDto.categories,
                    baseDto.languages,
                    Source = b.Source!,
                    b.StartPage,
                    b.EndPage
                };
                return Ok(article);

            case 3:
                var thesis = new 
                {
                    baseDto.BookId,
                    baseDto.Title,
                    baseDto.Description,
                    baseDto.DocumentTypeId,
                    baseDto.ddc,
                    baseDto.Publisher,
                    baseDto.PublishedYear,
                    baseDto.ImageUrl,
                    baseDto.authors,
                    baseDto.categories,
                    baseDto.languages,
                    totalCopies,
                    availableCopies,
                    FilePath = b.FilePath!,
                    b.FileSize,
                    b.IsPublic,
                    b.DownloadCount,

                    University = b.University!,
                    b.Faculty,
                    b.Advisor,
                    b.Degree,
                    b.DefenseYear
                };
                return Ok(thesis);

            case 4:
                var ebook = new 
                {
                    baseDto.BookId,
                    baseDto.Title,
                    baseDto.Description,
                    baseDto.DocumentTypeId,
                    baseDto.ddc,
                    baseDto.Publisher,
                    baseDto.PublishedYear,
                    baseDto.ImageUrl,
                    baseDto.authors,
                    baseDto.categories,
                    baseDto.languages,

                    FilePath = b.FilePath!,
                    b.FileSize,
                    b.IsPublic,
                    b.DownloadCount
                };
                return Ok(ebook);

            default:
                return Ok(baseDto);
        }
    }

    [HttpPost("book")]
    public async Task<IActionResult> CreatePhysicalBook([FromBody] CreatePhysicalBookDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required");

        if (string.IsNullOrWhiteSpace(dto.DDCCode))
            return BadRequest("DDC is required");

        var exists = await _context.Books
            .AnyAsync(b => b.ISBN == dto.ISBN && !b.IsDeleted);

        if (exists)
            return BadRequest("ISBN already exists");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var book = CreateBaseBook(dto, 1);

            book.ISBN = dto.ISBN;
            book.TotalPages = dto.TotalPages;
            book.Price = dto.Price;
            book.Location = dto.Location;
            book.IsBorrowable = dto.IsBorrowable ?? true;
            book.FilePath = dto.FilePath;
            book.FileSize = dto.FileSize;
            book.IsPublic = dto.IsPublic ?? false;
            book.DownloadCount = 0;

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            await AddRelations(book, dto);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(book.BookId);
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500);
        }
    }

    [HttpPost("article")]
    public async Task<IActionResult> CreateArticle([FromBody] CreateArticleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required");

        if (string.IsNullOrWhiteSpace(dto.Source))
            return BadRequest("Source is required");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var book = CreateBaseBook(dto, 2);

            book.Source = dto.Source;
            book.StartPage = dto.StartPage;
            book.EndPage = dto.EndPage;
            book.IsBorrowable = false;

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            await AddRelations(book, dto);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(book.BookId);
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500);
        }
    }

    [HttpPost("thesis")]
    public async Task<IActionResult> CreateThesis([FromBody] CreateThesisDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required");

        if (string.IsNullOrWhiteSpace(dto.University))
            return BadRequest("University is required");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var book = CreateBaseBook(dto, 3);

            book.University = dto.University;
            book.Faculty = dto.Faculty;
            book.Advisor = dto.Advisor;
            book.Degree = dto.Degree;
            book.DefenseYear = dto.DefenseYear;
            book.IsBorrowable = false;
            book.FilePath = dto.FilePath;
            book.FileSize = dto.FileSize;
            book.IsPublic = dto.IsPublic ?? false;
            
            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            await AddRelations(book, dto);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(book.BookId);
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500);
        }
    }

    [HttpPost("ebook")]
    public async Task<IActionResult> CreateEbook([FromBody] CreateEbookDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required");

        if (string.IsNullOrWhiteSpace(dto.FilePath))
            return BadRequest("FilePath is required");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var book = CreateBaseBook(dto, 4);

            book.FilePath = dto.FilePath;
            book.FileSize = dto.FileSize;
            book.IsPublic = dto.IsPublic ?? false;
            book.DownloadCount = 0;
            book.IsBorrowable = false;

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            await AddRelations(book, dto);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(book.BookId);
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500);
        }
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBook(
    int id,
    [ModelBinder(BinderType = typeof(UpdateBookDtoModelBinder))] BaseUpdateBookDto dto)
    {
        var errors = BookValidator.Validate(dto);
        if(errors.Any())
            return BadRequest(new { errors });

        var book = await _context.Books
            .Include(b => b.BookAuthors)
            .Include(b => b.BookCategories)
            .Include(b => b.BookLanguages)
            .FirstOrDefaultAsync(b => b.BookId == id);

        if (book == null)
            return NotFound();

        // ===== 1. COMMON =====
        if (dto.Title != null) book.Title = dto.Title;
        if (dto.Description != null) book.Description = dto.Description;
        if (dto.Publisher != null) book.Publisher = dto.Publisher;
        if (dto.PublishedYear.HasValue) book.PublishedYear = dto.PublishedYear;
        if (dto.ImageUrl != null) book.ImageUrl = dto.ImageUrl;
        if (dto.DDCCode != null) book.DDCCode = dto.DDCCode;

        book.DocumentTypeId = dto.DocumentTypeId;

        // ===== 2. AUTHORS =====
        if (dto.AuthorIds != null)
        {
            _context.BookAuthors.RemoveRange(book.BookAuthors);

            var authors = dto.AuthorIds.Select(a => new BookAuthor
            {
                BookId = id,
                AuthorId = a
            });

            await _context.BookAuthors.AddRangeAsync(authors);
        }

        // ===== 3. CATEGORIES =====
        if (dto.CategoryIds != null)
        {
            _context.BookCategories.RemoveRange(book.BookCategories);

            var categories = dto.CategoryIds.Select(c => new BookCategory
            {
                BookId = id,
                CategoryId = c
            });

            await _context.BookCategories.AddRangeAsync(categories);
        }

        // ===== 4. LANGUAGES =====
        if (dto.LanguageIds != null)
        {
            _context.BookLanguages.RemoveRange(book.BookLanguages);

            var languages = dto.LanguageIds.Select(l => new BookLanguage
            {
                BookId = id,
                LanguageId = l
            });

            await _context.BookLanguages.AddRangeAsync(languages);
        }

        // ===== 5. TYPE-SPECIFIC (NO DESERIALIZE) =====
        switch (dto)
        {
            case UpdatePhysicalBookDto physical:
                if (physical.ISBN != null) book.ISBN = physical.ISBN;
                if (physical.TotalPages != null) book.TotalPages = physical.TotalPages;
                if (physical.Price != null) book.Price = physical.Price;
                if (physical.IsBorrowable != null) book.IsBorrowable = physical.IsBorrowable;
                if (physical.Location != null) book.Location = physical.Location;
                if (physical.FilePath != null) book.FilePath = physical.FilePath;
                if (physical.FileSize != null) book.FileSize = physical.FileSize;
                if (physical.IsPublic != null) book.IsPublic = physical.IsPublic;
                break;

            case UpdateArticleDto article:
                if (article.Source != null) book.Source = article.Source;
                if (article.StartPage != null) book.StartPage = article.StartPage;
                if (article.EndPage != null) book.EndPage = article.EndPage;
                break;

            case UpdateThesisDto thesis:
                if (thesis.University != null) book.University = thesis.University;
                if (thesis.Faculty != null) book.Faculty = thesis.Faculty;
                if (thesis.Advisor != null) book.Advisor = thesis.Advisor;
                if (thesis.Degree != null) book.Degree = thesis.Degree;
                if (thesis.DefenseYear != null) book.DefenseYear = thesis.DefenseYear;
                if (thesis.FilePath != null) book.FilePath = thesis.FilePath;
                if (thesis.FileSize != null) book.FileSize = thesis.FileSize;
                if (thesis.IsPublic != null) book.IsPublic = thesis.IsPublic;
                break;

            case UpdateEbookDto ebook:
                if (ebook.FilePath != null) book.FilePath = ebook.FilePath;
                if (ebook.FileSize != null) book.FileSize = ebook.FileSize;
                if (ebook.IsPublic != null) book.IsPublic = ebook.IsPublic;
                break;

            default:
                return BadRequest("Unsupported document type");
        }

        await _context.SaveChangesAsync();
        var result = BookMapper.Map(book);
        
        return Ok(result);
    }

    // DELETE /api/Books/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _context.Books
            .Include(b => b.BookCopies)
            .FirstOrDefaultAsync(b => b.BookId == id && !b.IsDeleted);

        if (book == null)
            return NotFound(new { message = "Không tìm thấy sách" });

        // Do not allow deletion if there are copies currently borrowed
        var hasBorrowed = book.BookCopies
            .Any(c => c.Status == BookCopyStatus.Borrowed);

        if (hasBorrowed)
            return BadRequest(new { message = "Không thể xóa sách đang có bản sao được mượn" });

        var hasPendingRequest = await _context.BorrowRequests
            .AnyAsync(r => r.BookId == id &&
                    (r.Status == RequestStatus.Pending ||
                    r.Status == RequestStatus.Approved));

        if (hasPendingRequest)
            return BadRequest(new { message = "Không thể xóa sách đang có yêu cầu mượn chờ xử lý" });

        book.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đã xóa sách thành công" });
    }

    // GET /api/Books/new-arrivals — sách mới nhập
    [HttpGet("new-arrivals")]
    public async Task<IActionResult> GetNewArrivals()
    {
        var items = await _context.Books
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.CreatedDate)
            .Take(12)
            .Select(b => new
            {
                b.BookId, b.Title, b.ImageUrl, b.PublishedYear, b.DocumentTypeId,
                Authors         = b.BookAuthors.Select(ba => ba.Author.Name),
                AvailableCopies = b.BookCopies.Count(c => c.Status == BookCopyStatus.Available)
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET /api/Books/{id}/recommendations
    [HttpGet("{id}/recommendations")]
    public async Task<IActionResult> GetRecommendations(int id)
    {
        var book = await _context.Books
            .Include(b => b.BookCategories)
            .Include(b => b.BookAuthors)
            .FirstOrDefaultAsync(b => b.BookId == id && !b.IsDeleted);

        if (book == null)
            return NotFound();

        var categoryIds = book.BookCategories.Select(bc => bc.CategoryId).ToList();
        var authorIds   = book.BookAuthors.Select(ba => ba.AuthorId).ToList();

        // Score: same author = 3 pts, same genre = 2 pts, same DDC = 1 pt
        var candidates = await _context.Books
            .Include(b => b.BookAuthors)
            .Include(b => b.BookCategories)
            .Where(b => b.BookId != id && !b.IsDeleted &&
                (b.BookAuthors.Any(ba => authorIds.Contains(ba.AuthorId)) ||
                 b.BookCategories.Any(bc => categoryIds.Contains(bc.CategoryId)) ||
                 (book.DDCCode != null && b.DDCCode != null &&
                  b.DDCCode.Substring(0, Math.Min(3, b.DDCCode.Length)) ==
                  book.DDCCode.Substring(0, Math.Min(3, book.DDCCode.Length)))))
            .Select(b => new
            {
                b.BookId, b.Title, b.ImageUrl, b.PublishedYear,
                Authors    = b.BookAuthors.Select(ba => ba.Author.Name),
                SameAuthor   = b.BookAuthors.Any(ba => authorIds.Contains(ba.AuthorId)),
                SameCategory = b.BookCategories.Any(bc => categoryIds.Contains(bc.CategoryId)),
                SameDDC      = book.DDCCode != null && b.DDCCode != null &&
                               b.DDCCode.Substring(0, Math.Min(3, b.DDCCode.Length)) ==
                               book.DDCCode.Substring(0, Math.Min(3, book.DDCCode.Length)),
                AvailableCopies = b.BookCopies.Count(c => c.Status == BookCopyStatus.Available)
            })
            .Take(50)
            .ToListAsync();

        var scored = candidates
            .Select(b => new
            {
                b.BookId, b.Title, b.ImageUrl, b.PublishedYear,
                Authors         = b.Authors,
                b.AvailableCopies,
                Score = (b.SameAuthor ? 3 : 0) + (b.SameCategory ? 2 : 0) + (b.SameDDC ? 1 : 0)
            })
            .OrderByDescending(b => b.Score)
            .Take(10)
            .ToList();

        return Ok(scored);
    }
}