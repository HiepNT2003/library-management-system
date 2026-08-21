using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class BorrowRequest
{
    public int RequestId { get; set; }

    public string UserId { get; set; } = null!;

    public int BookId { get; set; }

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    public DateTime? ExpectedBorrowDate { get; set; } // Ngày dự kiến đến lấy sách
    
    public string? RejectedReason { get; set; }        // Lý do từ chối

    public RequestStatus Status { get; set; } = RequestStatus.Pending; 
    // Pending / Approved / Rejected / Cancelled

    public string? Note { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public string? ApprovedBy { get; set; } // StaffId

    public virtual Book Book { get; set; } = null!;

    public virtual ApplicationUser User { get; set; } = null!;
}

public enum RequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    Completed
}