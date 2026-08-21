using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class UserReadingHistory
{
    public int HistoryId { get; set; }

    public string UserId { get; set; } = null!;

    public int? BookId { get; set; }


    /// <summary>
    /// Read/Borrowed/Liked/Rated
    /// </summary>
    public string Action { get; set; } = null!;

    public int? DurationMinutes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Book? Book { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}
