using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class vw_BookAvailability
{
    public int BookId { get; set; }

    public string Title { get; set; } = null!;

    public string? BookLocation { get; set; }

    public int? TotalCopies { get; set; }

    public long AvailableCopies { get; set; }

    public string? AvailableLocations { get; set; }
}
