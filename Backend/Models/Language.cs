using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Language
{
    public int LanguageId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    public virtual ICollection<BookLanguage> BookLanguages { get; set; } = new List<BookLanguage>();
}
