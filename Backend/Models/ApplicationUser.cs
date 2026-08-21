using Backend.Models;
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public UserStatus? Status { get; set; } = UserStatus.Active;
    public string? FullName { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime? ExpiredDate { get; set; }

    public virtual ICollection<Fine> Fines { get; set; } = new List<Fine>();
    public virtual ICollection<ReadingProgress> ReadingProgresses { get; set; } = new List<ReadingProgress>();
    public virtual ICollection<Recomendation> Recomendations { get; set; } = new List<Recomendation>();

    public ICollection<Transaction> BorrowTransactions { get; set; } = new List<Transaction>();
    public ICollection<Transaction> ManagedTransactions { get; set; } = new List<Transaction>();

    public virtual ICollection<UserFavoriteBook> UserFavoriteBooks { get; set; } = new List<UserFavoriteBook>();
    public virtual ICollection<UserReadingHistory> UserReadingHistories { get; set; } = new List<UserReadingHistory>();

    public ICollection<BorrowRequest> BorrowRequests { get; set; } = new List<BorrowRequest>();
    public StudentProfile StudentProfile { get; set; }
    public StaffProfile StaffProfile { get; set; }
}
public enum UserStatus
{
    Active,
    Inactive,
    Blocked
}