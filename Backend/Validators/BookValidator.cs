public static class BookValidator
{
    public static Dictionary<string, string[]> Validate(BaseUpdateBookDto dto)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string field, string message)
        {
            if (!errors.ContainsKey(field))
                errors[field] = new List<string>();

            errors[field].Add(message);
        }

        // ===== COMMON =====
        if (dto.DocumentTypeId < 1 || dto.DocumentTypeId > 4)
            AddError("DocumentTypeId", "Invalid document type");

        if (dto.PublishedYear.HasValue &&
            (dto.PublishedYear < 1100 || dto.PublishedYear > DateTime.Now.Year))
        {
            AddError("PublishedYear", "Invalid year");
        }

        // ===== TYPE SPECIFIC =====
        switch (dto)
        {
            case UpdatePhysicalBookDto physical:
                if (string.IsNullOrWhiteSpace(physical.ISBN))
                    AddError("ISBN", "ISBN is required");

                if (physical.Price < 0)
                    AddError("Price", "Price must be >= 0");
                break;

            case UpdateArticleDto article:
                if (string.IsNullOrWhiteSpace(article.Source))
                    AddError("Source", "Source is required");

                if (article.StartPage > article.EndPage)
                    AddError("Page", "StartPage must be <= EndPage");
                break;

            case UpdateThesisDto thesis:
                if (string.IsNullOrWhiteSpace(thesis.University))
                    AddError("University", "University is required");
                break;

            case UpdateEbookDto ebook:
                if (string.IsNullOrWhiteSpace(ebook.FilePath))
                    AddError("FilePath", "FilePath is required");

                if (ebook.FileSize <= 0)
                    AddError("FileSize", "FileSize must be > 0");
                break;
        }

        return errors.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }
}