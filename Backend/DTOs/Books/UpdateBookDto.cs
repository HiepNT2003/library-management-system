using System.Text.Json.Serialization;
[JsonPolymorphic(TypeDiscriminatorPropertyName = "documentTypeId")]
[JsonDerivedType(typeof(UpdatePhysicalBookDto), 1)]
[JsonDerivedType(typeof(UpdateArticleDto), 2)]
[JsonDerivedType(typeof(UpdateThesisDto), 3)]
[JsonDerivedType(typeof(UpdateEbookDto), 4)]
public abstract class BaseUpdateBookDto
{
    public string? Title { get; set; }
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

public class UpdatePhysicalBookDto : BaseUpdateBookDto
{
    public string? ISBN { get; set; }
    public int? TotalPages { get; set; }
    public decimal? Price { get; set; }
    public bool? IsBorrowable { get; set; }
    public string? Location { get; set; }
    public string? FilePath { get; set; }
    public decimal? FileSize { get; set; }
    public bool? IsPublic { get; set; }
}

public class UpdateArticleDto : BaseUpdateBookDto
{
    public string? Source { get; set; }
    public int? StartPage { get; set; }
    public int? EndPage { get; set; }
}

public class UpdateThesisDto : BaseUpdateBookDto
{
    public string? University { get; set; }
    public string? Faculty { get; set; }
    public string? Advisor { get; set; }
    public string? Degree { get; set; }
    public int? DefenseYear { get; set; }
    public string? FilePath { get; set; }
    public decimal? FileSize { get; set; }
    public bool? IsPublic { get; set; }
}

public class UpdateEbookDto : BaseUpdateBookDto
{
    public string? FilePath { get; set; }
    public decimal? FileSize { get; set; }
    public bool? IsPublic { get; set; }
}

public class BaseBookResponseDto
{
    public int BookId { get; set; }
    public string? Title { get; set; }
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

public class PhysicalBookResponseDto : BaseBookResponseDto
{
    public string? ISBN { get; set; }
    public int? TotalPages { get; set; }
    public decimal? Price { get; set; }
    public bool? IsBorrowable { get; set; }
    public string? Location { get; set; }
    public string? FilePath { get; set; }
    public decimal? FileSize { get; set; }
    public bool? IsPublic { get; set; }
}

public class ArticleResponseDto : BaseBookResponseDto
{
    public string? Source { get; set; }
    public int? StartPage { get; set; }
    public int? EndPage { get; set; }
}

public class ThesisResponseDto : BaseBookResponseDto
{
    public string? University { get; set; }
    public string? Faculty { get; set; }
    public string? Advisor { get; set; }
    public string? Degree { get; set; }
    public int? DefenseYear { get; set; }
    public string? FilePath { get; set; }
    public decimal? FileSize { get; set; }
    public bool? IsPublic { get; set; }
}

public class EbookResponseDto : BaseBookResponseDto
{
    public string? FilePath { get; set; }
    public decimal? FileSize { get; set; }
    public bool? IsPublic { get; set; }
}