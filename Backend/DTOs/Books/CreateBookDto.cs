public class CreateBookDto
{
    public string Title { get; set; }
    public string? ISBN { get; set; }
    public string? Publisher { get; set; }
    public short? PublishedYear { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? Price { get; set; }

    public int? DocumentTypeId { get; set; } 
    public string? DDCCode { get; set; }     
    public bool IsBorrowable { get; set; }
    public int? TotalPages { get; set; }

    public List<int>? AuthorIds { get; set; }
    public List<int>? CategoryIds { get; set; }
    public List<int>? LanguageIds { get; set; }
}
public class BaseCreateBookDto
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int DocumentTypeId { get; set; }
    public string? DDCCode { get; set; }
    public string? Publisher { get; set; }
    public short? PublishedYear { get; set; }
    public string? ImageUrl { get; set; }

    public List<int>? AuthorIds { get; set; }
    public List<int>? CategoryIds { get; set; }
    public List<int>? LanguageIds { get; set; }
}
public class CreatePhysicalBookDto : BaseCreateBookDto
{
    public string ISBN { get; set; } = null!;
    public int? TotalPages { get; set; }
    public decimal? Price { get; set; }
    public bool? IsBorrowable { get; set; } = true;
    public string? Location { get; set; }
    public string? FilePath { get; set; } = null!;
    public decimal? FileSize { get; set; }
    public bool? IsPublic { get; set; }
}
public class CreateArticleDto : BaseCreateBookDto
{
    public string Source { get; set; } = null!;
    public int? StartPage { get; set; }
    public int? EndPage { get; set; }
}
public class CreateThesisDto : BaseCreateBookDto
{
    public string University { get; set; } = null!;
    public string? Faculty { get; set; }
    public string? Advisor { get; set; }
    public string? Degree { get; set; }
    public int? DefenseYear { get; set; }
    public string? FilePath { get; set; } = null!;
    public decimal? FileSize { get; set; }
    public bool? IsPublic { get; set; }
}
public class CreateEbookDto : BaseCreateBookDto
{
    public string FilePath { get; set; } = null!;
    public decimal? FileSize { get; set; }
    public bool? IsPublic { get; set; }
}