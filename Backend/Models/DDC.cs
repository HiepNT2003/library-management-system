using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class DDC
{
    public string Code { get; set; } // 500, 510, 512...
    public string Name { get; set; } // Khoa học, Toán học...
    public string? ParentCode { get; set; }
    public DDC Parent { get; set; }

    public ICollection<DDC> Children { get; set; }
    public ICollection<Book> Books { get; set; } = new List<Book>();
}