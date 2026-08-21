using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class ReadingProgress
{
    public int ProgressId { get; set; }

    public int BookId { get; set; }

    public string? UserId { get; set; }

    public int? CurrentPage { get; set; } = 0;

    public decimal? PercentRead { get; set; }

    public DateTime? LastReadDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// [{&quot;page&quot;:45, &quot;text&quot;: &quot;highlight&quot;}]
    /// </summary>
    public string? HighLights { get; set; }

    public string? Notes { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual ApplicationUser User { get; set; }
}
