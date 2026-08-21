using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Recomendation
{
    public int RecId { get; set; }

    public string UserId { get; set; } = null!;

    public int BookId { get; set; }

    /// <summary>
    /// 0.000-1.000 similarity
    /// </summary>
    public decimal Score { get; set; }

    /// <summary>
    /// AI explanation
    /// </summary>
    public string? Reason { get; set; }

    public DateTime? GeneratedDate { get; set; } = DateTime.UtcNow;

    public bool IsViewed { get; set; } = false;

    public virtual Book? Book { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}
