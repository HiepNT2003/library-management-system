using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    public virtual ICollection<BookCategory> BookCategories { get; set; } = new List<BookCategory>();
}
