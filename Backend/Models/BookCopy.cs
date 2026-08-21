using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class BookCopy
{
    public int CopyId { get; set; }

    public int? BookId { get; set; }

    public string? Barcode { get; set; }

    public BookCopyStatus Status { get; set; } = BookCopyStatus.Available;

    public string? ShelfLocation { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public string? BookCondition { get; set; }

    public bool IsReferenceOnly { get; set; }

    public string? Notes { get; set; }

    public int WarehouseId { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;

    public virtual Book? Book { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<BookCopyStatusHistory> StatusHistories { get; set; } = new List<BookCopyStatusHistory>();
}

public enum BookCopyStatus
{
    Available,
    Borrowed,
    Lost,
    Damaged
}