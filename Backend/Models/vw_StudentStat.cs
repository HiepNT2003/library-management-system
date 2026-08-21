using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class vw_StudentStat
{
    public string Id { get; set; } = null!;

    public string? FullName { get; set; }

    public string? StudentClass { get; set; }

    public long TotalBorrows { get; set; }

    public decimal? TotalFines { get; set; }

    /// <summary>
    /// LIBRARY: Active/Suspended/Expired/Blacklisted
    /// </summary>
    public string? LibraryStatus { get; set; }
}
