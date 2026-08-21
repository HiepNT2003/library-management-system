using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public partial class Book
{
    public int BookId { get; set; }

    public string Title { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? Description { get; set; }

    public int DocumentTypeId { get; set; }

    public string? DDCCode { get; set; }

    public string? Publisher { get; set; }

    public short? PublishedYear { get; set; }

    public string? ImageUrl { get; set; }

    // === Book ===
    public string? ISBN { get; set; }

    public int? TotalPages { get; set; }

    public decimal? Price { get; set; }

    public bool? IsBorrowable { get; set; } = true;

    public string? Location { get; set; }

    // ===Article===
    public string? Source { get; set; }

    public int? StartPage { get; set; }

    public int? EndPage { get; set; }

    // === Thesis ===
    public string? University { get; set; }
    public string? Faculty { get; set; }

    public string? Advisor { get; set; }

    public string? Degree { get; set; }
    
    public int? DefenseYear { get; set; }

    // === Ebook ===

    public string? FilePath { get; set; }

    public decimal? FileSize { get; set; }

    public int? DownloadCount { get; set; }

    public bool? IsPublic { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? CreatedDate { get; set; }

    public DocumentType DocumentType { get; set; }

    public virtual DDC DDC { get; set; }

    public virtual ICollection<BookCopy> BookCopies { get; set; } = new List<BookCopy>();

    public virtual ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
    public virtual ICollection<BookLanguage> BookLanguages { get; set; } = new List<BookLanguage>();

    public virtual ICollection<Recomendation> Recomendations { get; set; } = new List<Recomendation>();

    public virtual ICollection<UserFavoriteBook> UserFavoriteBooks { get; set; } = new List<UserFavoriteBook>();

    public virtual ICollection<UserReadingHistory> UserReadingHistories { get; set; } = new List<UserReadingHistory>();

    public virtual ICollection<ReadingProgress> ReadingProgresses { get; set; } = new List<ReadingProgress>();

    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
    
    public ICollection<BorrowRequest> BorrowRequests { get; set; }
}
