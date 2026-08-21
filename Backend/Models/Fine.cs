using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Fine
{
    public int FineId { get; set; }

    public int TransactionId { get; set; }

    public decimal Amount { get; set; }

    public string? Reason { get; set; }
    public string? Note { get; set; }
    public DateTime? CreatedDate { get; set; }

    public DateTime? PaidDate { get; set; }

    public FineStatus? Status { get; set; } = FineStatus.Pending;

    public string? PaidByUserId { get; set; }

    public virtual ApplicationUser? PaidByUser { get; set; }

    public virtual Transaction? Transaction { get; set; }
}

public enum FineStatus
{
    Pending,
    Paid,
    Waived
}