using Backend.Models;

public static class BookMapper
{
    public static BaseBookResponseDto Map(Book book)
    {
        var baseDto = new BaseBookResponseDto
        {
            BookId = book.BookId,
            Title = book.Title,
            Description = book.Description,
            DocumentTypeId = book.DocumentTypeId,
            DDCCode = book.DDCCode,
            Publisher = book.Publisher,
            PublishedYear = book.PublishedYear,
            ImageUrl = book.ImageUrl,

            AuthorIds = book.BookAuthors.Select(x => x.AuthorId).ToList(),
            CategoryIds = book.BookCategories.Select(x => x.CategoryId).ToList(),
            LanguageIds = book.BookLanguages.Select(x => x.LanguageId).ToList()
        };

        return book.DocumentTypeId switch
        {
            1 => new PhysicalBookResponseDto
            {
                // base
                BookId = baseDto.BookId,
                Title = baseDto.Title,
                Description = baseDto.Description,
                DocumentTypeId = baseDto.DocumentTypeId,
                DDCCode = baseDto.DDCCode,
                Publisher = baseDto.Publisher,
                PublishedYear = baseDto.PublishedYear,
                ImageUrl = baseDto.ImageUrl,
                AuthorIds = baseDto.AuthorIds,
                CategoryIds = baseDto.CategoryIds,
                LanguageIds = baseDto.LanguageIds,

                // extra
                ISBN = book.ISBN,
                TotalPages = book.TotalPages,
                Price = book.Price,
                IsBorrowable = book.IsBorrowable,
                Location = book.Location
            },

            2 => new ArticleResponseDto
            {
                BookId = baseDto.BookId,
                Title = baseDto.Title,
                Description = baseDto.Description,
                DocumentTypeId = baseDto.DocumentTypeId,
                DDCCode = baseDto.DDCCode,
                Publisher = baseDto.Publisher,
                PublishedYear = baseDto.PublishedYear,
                ImageUrl = baseDto.ImageUrl,
                AuthorIds = baseDto.AuthorIds,
                CategoryIds = baseDto.CategoryIds,
                LanguageIds = baseDto.LanguageIds,

                Source = book.Source,
                StartPage = book.StartPage,
                EndPage = book.EndPage
            },

            3 => new ThesisResponseDto
            {
                BookId = baseDto.BookId,
                Title = baseDto.Title,
                Description = baseDto.Description,
                DocumentTypeId = baseDto.DocumentTypeId,
                DDCCode = baseDto.DDCCode,
                Publisher = baseDto.Publisher,
                PublishedYear = baseDto.PublishedYear,
                ImageUrl = baseDto.ImageUrl,
                AuthorIds = baseDto.AuthorIds,
                CategoryIds = baseDto.CategoryIds,
                LanguageIds = baseDto.LanguageIds,

                University = book.University,
                Faculty = book.Faculty,
                Advisor = book.Advisor,
                Degree = book.Degree,
                DefenseYear = book.DefenseYear
            },

            4 => new EbookResponseDto
            {
                BookId = baseDto.BookId,
                Title = baseDto.Title,
                Description = baseDto.Description,
                DocumentTypeId = baseDto.DocumentTypeId,
                DDCCode = baseDto.DDCCode,
                Publisher = baseDto.Publisher,
                PublishedYear = baseDto.PublishedYear,
                ImageUrl = baseDto.ImageUrl,
                AuthorIds = baseDto.AuthorIds,
                CategoryIds = baseDto.CategoryIds,
                LanguageIds = baseDto.LanguageIds,

                FilePath = book.FilePath,
                FileSize = book.FileSize,
                IsPublic = book.IsPublic
            },

            _ => baseDto
        };
    }
}