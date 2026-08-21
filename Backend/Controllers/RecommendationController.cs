using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IRecommendationService _recommendationService;


    public RecommendationController(AppDbContext context, IRecommendationService recommendationService)
    {
        _context = context;
        _recommendationService = recommendationService;
    }

     // GET /api/recommendation/personal — suggest based on borrowing history
    [HttpGet("personal")]
    [Authorize]
    public async Task<IActionResult> GetPersonalRecommendations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Take the borrowed book to exclude
        var borrowedBookIds = await _context.Transactions
            .Where(t => t.UserId == userId && t.Copy != null && t.Copy.BookId != null)
            .Select(t => t.Copy!.BookId!.Value)
            .Distinct()
            .ToListAsync();

        List<(int BookId, float Score)> mlResults = new();

        // Use ML.NET if model is trained
        if (_recommendationService.IsModelTrained)
        {
            mlResults = _recommendationService
                .Predict(userId, topN: 20)
                .Where(x => !borrowedBookIds.Contains(x.BookId))
                .Take(10)
                .ToList();
        }

        // Fallback: rule-based if you don't have a model yet or don't have enough results
        if (mlResults.Count < 5)
        {
            var ruleBasedResult = await GetRuleBasedRecommendations(userId, borrowedBookIds);
            return Ok(ruleBasedResult);
        }

        // Get the full book information
        var bookIds = mlResults.Select(x => x.BookId).ToList();
        var books   = await _context.Books
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Where(b => bookIds.Contains(b.BookId) && !b.IsDeleted)
            .Select(b => new
            {
                b.BookId, b.Title, b.ImageUrl, b.PublishedYear,
                Authors         = b.BookAuthors.Select(ba => ba.Author.Name),
                AvailableCopies = b.BookCopies.Count(c => c.Status == BookCopyStatus.Available),
            })
            .ToListAsync();

        var result = mlResults
            .Select(r => new
            {
                r.BookId,
                r.Score,
                Reason = "Dựa theo lịch sử mượn của bạn",
                Book   = books.FirstOrDefault(b => b.BookId == r.BookId)
            })
            .Where(x => x.Book != null)
            .ToList();

        return Ok(result);
    }

    // Rule-based fallback
    private async Task<object> GetRuleBasedRecommendations(string userId, List<int> borrowedBookIds)
    {
        var interactedBooks = await _context.Books
            .Include(b => b.BookCategories)
            .Include(b => b.BookAuthors)
            .Where(b => borrowedBookIds.Contains(b.BookId))
            .ToListAsync();

        if (!interactedBooks.Any())
        {
            // No history → show popular books
            return await _context.Books
                .Where(b => !b.IsDeleted)
                .OrderByDescending(b => b.BookCopies.SelectMany(c => c.Transactions).Count())
                .Take(10)
                .Select(b => new
                {
                    b.BookId, b.Title, b.ImageUrl, b.PublishedYear,
                    Authors         = b.BookAuthors.Select(ba => ba.Author.Name),
                    AvailableCopies = b.BookCopies.Count(c => c.Status == BookCopyStatus.Available),
                    Reason          = "Sách được mượn nhiều nhất"
                })
                .ToListAsync();
        }

        var preferredCategoryIds = interactedBooks
            .SelectMany(b => b.BookCategories.Select(bc => bc.CategoryId))
            .GroupBy(id => id).OrderByDescending(g => g.Count())
            .Take(3).Select(g => g.Key).ToList();

        var preferredAuthorIds = interactedBooks
            .SelectMany(b => b.BookAuthors.Select(ba => ba.AuthorId))
            .GroupBy(id => id).OrderByDescending(g => g.Count())
            .Take(3).Select(g => g.Key).ToList();

        return await _context.Books
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookCategories)
            .Where(b => !b.IsDeleted && !borrowedBookIds.Contains(b.BookId) &&
                        (b.BookCategories.Any(bc => preferredCategoryIds.Contains(bc.CategoryId)) ||
                        b.BookAuthors.Any(ba => preferredAuthorIds.Contains(ba.AuthorId))))
            .Take(10)
            .Select(b => new
            {
                b.BookId, b.Title, b.ImageUrl, b.PublishedYear,
                Authors         = b.BookAuthors.Select(ba => ba.Author.Name),
                AvailableCopies = b.BookCopies.Count(c => c.Status == BookCopyStatus.Available),
                Reason          = b.BookAuthors.Any(ba => preferredAuthorIds.Contains(ba.AuthorId))
                                ? "Tác giả bạn yêu thích"
                                : "Thể loại bạn quan tâm"
            })
            .ToListAsync();
    }
}