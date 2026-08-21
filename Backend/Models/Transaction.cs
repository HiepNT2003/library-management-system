using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public string? UserId { get; set; }

    public int? CopyId { get; set; }

    public string? LibrarianId { get; set; }        // thủ thư cho mượn

    public int? RequestId { get; set; }

    public DateTime BorrowDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public int ExtensionCount { get; set; } = 0;

    public TransactionStatus Status { get; set; } = TransactionStatus.Borrowed;  // đổi thành enum

    public string? Notes { get; set; }

    public string? ReturnCondition { get; set; }    // thêm mới: Tốt/Hư/Mất

    public string? ReturnLibrarianId { get; set; }  // thêm mới

    public virtual BookCopy? Copy { get; set; }

    public virtual BorrowRequest? Request { get; set; }  // thêm mới

    public virtual ICollection<Fine> Fines { get; set; } = new List<Fine>();

    public virtual ApplicationUser? User { get; set; }

    public virtual ApplicationUser? Librarian { get; set; }

    public virtual ApplicationUser? ReturnLibrarian { get; set; }  // thêm mới
}

public enum TransactionStatus
{
    Borrowed,
    Returned,
    Overdue,
    Cancelled // no use
}