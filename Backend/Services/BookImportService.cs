using System.Text.Json;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class BookImportService
{
    private readonly AppDbContext _context;
    private readonly HttpClient _httpClient;

    public BookImportService(AppDbContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    public async Task ImportBooks(string query, int pages = 3)
    {
        var random = new Random();

        for (int page = 0; page < pages; page++)
        {
            int startIndex = page * 40;
            var url = $"https://www.googleapis.com/books/v1/volumes?q={query}&maxResults=40&startIndex={startIndex}&key=";

            var response = await _httpClient.GetStringAsync(url);

            using var json = JsonDocument.Parse(response);

            if (!json.RootElement.TryGetProperty("items", out var items))
                continue;

            foreach (var item in items.EnumerateArray())
            {
                var volume = item.GetProperty("volumeInfo");

                string title = volume.GetProperty("title").GetString() ?? "Unknown";

                // ===== ISBN =====
                string? isbn = null;

                if (volume.TryGetProperty("industryIdentifiers", out var identifiers))
                {
                    foreach (var id in identifiers.EnumerateArray())
                    {
                        var type = id.GetProperty("type").GetString();

                        if (type == "ISBN_13")
                        {
                            isbn = id.GetProperty("identifier").GetString();
                            break;
                        }
                    }

                    if (isbn == null && identifiers.GetArrayLength() > 0)
                        isbn = identifiers[0].GetProperty("identifier").GetString();
                }

                // ===== CHECK DUPLICATE =====
                bool exist = !string.IsNullOrEmpty(isbn)
                    ? await _context.Books.AnyAsync(b => b.ISBN == isbn)
                    : await _context.Books.AnyAsync(b => b.Title == title);

                if (exist)
                    continue;

                // ===== PUBLISHED YEAR =====
                short? year = null;

                if (volume.TryGetProperty("publishedDate", out var date))
                {
                    var dateStr = date.GetString();

                    if (!string.IsNullOrWhiteSpace(dateStr) && dateStr.Length >= 4)
                    {
                        var yearPart = dateStr.Substring(0, 4);

                        if (short.TryParse(yearPart, out short parsedYear))
                        {
                            if (parsedYear >= 1901 && parsedYear <= 2155)
                            {
                                year = parsedYear;
                            }
                        }
                    }
                }

                // ===== TOTAL PAGES =====
                int? totalPages = null;

                if (volume.TryGetProperty("pageCount", out var pagesEl))
                {
                    if (pagesEl.TryGetInt32(out int p))
                    {
                        totalPages = p;
                    }
                }

                // ===== PRICE =====
                decimal? price = null;

                if (item.TryGetProperty("saleInfo", out var saleInfo))
                {
                    if (saleInfo.TryGetProperty("listPrice", out var listPrice))
                    {
                        if (listPrice.TryGetProperty("amount", out var amount))
                        {
                            if (amount.TryGetDecimal(out decimal p))
                            {
                                price = p;
                            }
                        }
                    }
                }

                // fallback nếu không có price
                if (price == null)
                {
                    price = random.Next(5, 50); // random giá giả
                }

                // ===== OTHER FIELDS =====
                string? description = volume.TryGetProperty("description", out var desc)
                    ? desc.GetString()
                    : null;

                string? language = volume.TryGetProperty("language", out var lang)
                    ? lang.GetString()
                    : null;

                string? publisher = volume.TryGetProperty("publisher", out var pub)
                    ? pub.GetString()
                    : null;

                string? imageUrl = null;

                if (volume.TryGetProperty("imageLinks", out var img))
                {
                    imageUrl = img.TryGetProperty("thumbnail", out var thumb)
                        ? thumb.GetString()
                        : null;
                }

                 // ===== CATEGORY → DDC =====
            string ddcCode = "000";

            if (volume.TryGetProperty("categories", out var lcategories))
            {
                foreach (var cat in lcategories.EnumerateArray())
                {
                    var name = cat.GetString()?.ToLower();

                    if (name.Contains("computer"))
                        ddcCode = "005";
                    else if (name.Contains("science"))
                        ddcCode = "500";
                    else if (name.Contains("art"))
                        ddcCode = "700";
                    else if (name.Contains("technology"))
                        ddcCode = "600";
                    else if (name.Contains("history"))
                        ddcCode = "900";
                }
            }

                // ===== CREATE BOOK =====
                var book = new Book
                {
                    Title = title,
                    ISBN = isbn,
                    Publisher = publisher,
                    Description = description,
                    ImageUrl = imageUrl,
                    PublishedYear = year,
                    TotalPages = totalPages,
                    DDCCode = ddcCode,
                    DocumentTypeId = 1,
                    Price = price,
                    Location = "T6",
                    CreatedDate = DateTime.UtcNow
                };

                _context.Books.Add(book);

                // ========================
                // AUTHORS
                // ========================
                if (volume.TryGetProperty("authors", out var authors))
                {
                    foreach (var authorEl in authors.EnumerateArray())
                    {
                        string authorName = authorEl.GetString();

                        if (string.IsNullOrEmpty(authorName))
                            continue;

                        var author = await _context.Authors
                            .FirstOrDefaultAsync(a => a.Name == authorName);

                        if (author == null)
                        {
                            author = new Author { Name = authorName };
                            _context.Authors.Add(author);
                            await _context.SaveChangesAsync();
                        }

                        _context.BookAuthors.Add(new BookAuthor
                        {
                            Book = book,
                            Author = author
                        });
                    }
                }

                // ========================
                // CATEGORY
                // ========================
                string categoryCode = "000";

                if (volume.TryGetProperty("categories", out var categories))
                {
                    foreach (var cat in categories.EnumerateArray())
                    {
                        string categoryName = cat.GetString();

                        if (string.IsNullOrEmpty(categoryName))
                            continue;

                        var category = await _context.Categories
                            .FirstOrDefaultAsync(c => c.Name == categoryName);

                        if (category == null)
                        {
                            category = new Category { Name = categoryName };
                            _context.Categories.Add(category);
                            await _context.SaveChangesAsync();
                        }

                        categoryCode = category.CategoryId.ToString("D3");

                        _context.BookCategories.Add(new BookCategory
                        {
                            Book = book,
                            CategoryId = category.CategoryId
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // ========================
                // BOOK COPIES
                // ========================
                for (int i = 1; i <= 10; i++)
                {
                    _context.BookCopies.Add(new BookCopy
                    {
                        BookId = book.BookId,
                        Barcode = $"BC-{Guid.NewGuid().ToString().Substring(0, 8)}",
                        ShelfLocation = $"{categoryCode}-A1-{i}",
                        Status = BookCopyStatus.Available
                    });
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}