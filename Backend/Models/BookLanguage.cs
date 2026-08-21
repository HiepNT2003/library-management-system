using System;
using System.Collections.Generic;

namespace Backend.Models;

public class BookLanguage
{
    public int BookLanguageId { get; set; }
    public int BookId { get; set; }
    public int LanguageId { get; set; }

    public Book Book { get; set; }
    public Language Language { get; set; }
}