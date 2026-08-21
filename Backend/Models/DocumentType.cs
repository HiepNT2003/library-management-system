using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class DocumentType
{
    public int DocumentTypeId { get; set; }
    public string Name { get; set; }

    public ICollection<Book> Books { get; set; }
}
