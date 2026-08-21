namespace Backend.Models;

public class Notification
{
    public int NotificationId { get; set; }
    public string UserId      { get; set; } = null!;
    public string Title       { get; set; } = null!;
    public string? Message    { get; set; }
    public string Type        { get; set; } = null!;
    public string? Link       { get; set; }
    public bool IsRead        { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual ApplicationUser User { get; set; } = null!;
}

public static class NotificationType
{
    public const string BorrowApproved = "BorrowApproved";
    public const string BorrowRejected = "BorrowRejected";
    public const string DueSoon        = "DueSoon";
    public const string Overdue        = "Overdue";
    public const string FineCreated    = "FineCreated";
    public const string FineWaived     = "FineWaived";
    public const string ExtendSuccess  = "ExtendSuccess";
    public const string NewRequest     = "NewRequest";
    public const string System         = "System";
}