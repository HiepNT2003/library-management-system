using System;
using System.Collections.Generic;

namespace Backend.Models;

public class Warehouse
{
    public int WarehouseId { get; set; }
    public string Name { get; set; }       // "Phòng Mượn"
    public string Code { get; set; }       // "PM"
    public string? Location { get; set; }
    public string? Description { get; set; }
    public ICollection<BookCopy> BookCopies { get; set; } = new List<BookCopy>();
}
