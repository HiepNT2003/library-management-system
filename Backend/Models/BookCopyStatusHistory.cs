using System;
using System.Collections.Generic;

namespace Backend.Models;

public class BookCopyStatusHistory
{
    public int Id { get; set; }
    public int CopyId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = "";
    public string? ChangedById { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;
    public string? Reason { get; set; }

    public virtual BookCopy Copy { get; set; }
    public virtual ApplicationUser? ChangedBy { get; set; }
}
