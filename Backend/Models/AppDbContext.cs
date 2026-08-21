using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Models;

public partial class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<RefreshToken> RefreshTokens {get; set;}

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Book> Books { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<BookCopy> BookCopies { get; set; }

    public virtual DbSet<BookCopyStatusHistory> BookCopyStatusHistories { get; set; }

    public virtual DbSet<BookCategory> BookCategories { get; set; }

    public virtual DbSet<BorrowRequest> BorrowRequests { get; set; }

    public virtual DbSet<BorrowPolicy> BorrowPolicies { get; set; }

    public virtual DbSet<DDC> DDCs { get; set; }

    public virtual DbSet<BookAuthor> BookAuthors { get; set; }

    public virtual DbSet<DocumentType> DocumentTypes { get; set; }

    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Language> Languages { get; set; }
    public virtual DbSet<BookLanguage> BookLanguages { get; set; }

    public virtual DbSet<Fine> Fines { get; set; }

    public virtual DbSet<ReadingProgress> ReadingProgresses { get; set; }

    public virtual DbSet<Recomendation> Recomendations { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<UserFavoriteBook> UserFavoriteBooks { get; set; }

    public virtual DbSet<UserReadingHistory> UserReadingHistories { get; set; }
    public virtual DbSet<StudentProfile> StudentProfiles { get; set; }
    public virtual DbSet<StaffProfile> StaffProfiles { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<vw_BookAvailability> vw_BookAvailabilities { get; set; }

    public virtual DbSet<vw_StudentStat> vw_StudentStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.AuthorId).HasName("PRIMARY");

            entity.HasIndex(e => e.Name, "idx_Name");

            entity.Property(e => e.AuthorId).HasColumnType("int(11)");
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("PRIMARY");

            entity.HasIndex(e => e.ISBN, "idx_ISBN").IsUnique();

            entity.HasIndex(e => e.Title, "idx_Title");

            entity.Property(e => e.BookId).HasColumnType("int(11)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ISBN).HasMaxLength(50);
            entity.Property(e => e.DDCCode).HasMaxLength(50);
            entity.Property(e => e.TotalPages)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.FileSize).HasPrecision(10, 2);
            entity.Property(e => e.PublishedYear);
            entity.Property(e => e.Publisher).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(300);
            // entity.Property(e => e.TotalCopies)
            //     .HasDefaultValueSql("'1'")
            //     .HasColumnType("int(11)");
        });
        modelBuilder.Entity<BookCategory>()
            .HasKey(bc => new { bc.BookId, bc.CategoryId });

        modelBuilder.Entity<BookCategory>()
            .HasOne(bc => bc.Book)
            .WithMany(b => b.BookCategories)
            .HasForeignKey(bc => bc.BookId);

        modelBuilder.Entity<BookCategory>()
            .HasOne(bc => bc.Category)
            .WithMany(c => c.BookCategories)
            .HasForeignKey(bc => bc.CategoryId);
                modelBuilder.Entity<BookCategory>()
            .HasKey(bc => new { bc.BookId, bc.CategoryId });

         modelBuilder.Entity<BookAuthor>()
        .HasKey(ba => new { ba.BookId, ba.AuthorId });

        modelBuilder.Entity<BookAuthor>()
            .HasOne(ba => ba.Book)
            .WithMany(b => b.BookAuthors)
            .HasForeignKey(ba => ba.BookId);

        modelBuilder.Entity<BookAuthor>()
            .HasOne(ba => ba.Author)
            .WithMany(a => a.BookAuthors)
            .HasForeignKey(ba => ba.AuthorId);
        modelBuilder.Entity<BookCopy>(entity =>
        {
            entity.HasKey(e => e.CopyId).HasName("PRIMARY");

            entity.HasIndex(e => e.Barcode, "Barcode").IsUnique();

            entity.HasIndex(e => e.BookId, "idx_BookId");

            entity.HasIndex(e => e.Status, "idx_Status");

            entity.Property(e => e.CopyId).HasColumnType("int(11)");
            entity.Property(e => e.Barcode).HasMaxLength(50);
            entity.Property(e => e.BookCondition)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Good'");
            entity.Property(e => e.BookId).HasColumnType("int(11)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ShelfLocation).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasConversion<int>()
                .HasDefaultValue(BookCopyStatus.Available);

            entity.HasOne(d => d.Book).WithMany(p => p.BookCopies)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("BookCopies_ibfk_1");
            entity.HasOne(bc => bc.Warehouse)
                .WithMany(w => w.BookCopies)
                .HasForeignKey(bc => bc.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<DocumentType>()
        .Property(x => x.DocumentTypeId)
        .ValueGeneratedNever();

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PRIMARY");

            entity.Property(e => e.CategoryId).HasColumnType("int(11)");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<BookLanguage>()
        .HasKey(bl => new { bl.BookId, bl.LanguageId });

        modelBuilder.Entity<BookLanguage>()
            .HasOne(bl => bl.Book)
            .WithMany(b => b.BookLanguages)
            .HasForeignKey(bl => bl.BookId);

        modelBuilder.Entity<BookLanguage>()
            .HasOne(bl => bl.Language)
            .WithMany(l => l.BookLanguages)
            .HasForeignKey(bl => bl.LanguageId);

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");
            entity.HasOne(e => e.User)
                .WithOne(u => u.StudentProfile)
                .HasForeignKey<StudentProfile>(e => e.UserId);
        });

        modelBuilder.Entity<StaffProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");
            entity.HasOne(e => e.User)
                .WithOne(u => u.StaffProfile)
                .HasForeignKey<StaffProfile>(e => e.UserId);
        });

        modelBuilder.Entity<Fine>(entity =>
        {
            entity.HasKey(e => e.FineId).HasName("PRIMARY");

            entity.HasIndex(e => e.PaidByUserId, "PaidByUserId");

            entity.HasIndex(e => e.Status, "idx_Status");

            entity.HasIndex(e => e.TransactionId, "idx_TransactionId");

            entity.Property(e => e.FineId).HasColumnType("int(11)");
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.PaidByUserId).HasMaxLength(450);
            entity.Property(e => e.PaidDate).HasColumnType("datetime");
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.Status)
                .HasConversion<int>()
                .HasDefaultValue(FineStatus.Pending);
            entity.Property(e => e.TransactionId).HasColumnType("int(11)");

            entity.HasOne(d => d.PaidByUser) // ✅ navigation
                .WithMany(p => p.Fines)
                .HasForeignKey(d => d.PaidByUserId) // ✅ FK
                .HasConstraintName("Fines_ibfk_2");

            entity.HasOne(d => d.Transaction).WithMany(p => p.Fines)
                .HasForeignKey(d => d.TransactionId)
                .HasConstraintName("Fines_ibfk_1");
        });

        modelBuilder.Entity<BorrowRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BookId);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.RequestDate)
                .HasColumnType("datetime");

            entity.Property(e => e.Status)
                .HasConversion<int>()
                .HasDefaultValue(RequestStatus.Pending);

            entity.Property(e => e.Note)
                .HasMaxLength(500);

            entity.Property(e => e.ApprovedDate)
                .HasColumnType("datetime");

            entity.Property(e => e.ApprovedBy)
                .HasMaxLength(450);

            entity.HasOne(d => d.User)
                .WithMany(u => u.BorrowRequests)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Book)
                .WithMany(b => b.BorrowRequests)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(d => d.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

         modelBuilder.Entity<BorrowPolicy>()
            .HasOne(bp => bp.Role)
            .WithMany()
            .HasForeignKey(bp => bp.AspNetRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BorrowPolicy>()
            .HasOne(bp => bp.DocumentType)
            .WithMany()
            .HasForeignKey(bp => bp.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BorrowPolicy>()
            .HasIndex(bp => new { bp.AspNetRoleId, bp.DocumentTypeId })
            .IsUnique();

        modelBuilder.Entity<DDC>(entity =>
        {
            entity.HasKey(e => e.Code).HasName("PRIMARY");

            entity.Property(e => e.Code)
                .HasMaxLength(20);

            entity.Property(e => e.Name)
                .HasMaxLength(255);

            entity.HasMany(d => d.Books)
                .WithOne(b => b.DDC)
                .HasForeignKey(b => b.DDCCode);
        });
        modelBuilder.Entity<DDC>().ToTable("DDC");

        modelBuilder.Entity<ReadingProgress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("PRIMARY");

            entity.ToTable("ReadingProgress");

            entity.HasIndex(e => e.BookId, "BookId");

            entity.HasIndex(e => e.LastReadDate, "idx_LastReadDate");

            entity.HasIndex(e => e.UserId, "idx_UserId");

            entity.HasIndex(e => new { e.UserId, e.BookId }, "unique_user_book").IsUnique();

            entity.Property(e => e.ProgressId).HasColumnType("int(11)");
            entity.Property(e => e.CurrentPage)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)");
            entity.Property(e => e.BookId).HasColumnType("int(11)");
            entity.Property(e => e.HighLights)
                .HasComment("[{\"page\":45, \"text\": \"highlight\"}]")
                .HasColumnType("json");
            entity.Property(e => e.LastReadDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.Notes).HasColumnType("text");
            entity.Property(e => e.PercentRead)
                .HasPrecision(5, 2)
                .HasDefaultValueSql("'0.00'");
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.Book).WithMany(p => p.ReadingProgresses)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ReadingProgress_ibfk_1");

            entity.HasOne(d => d.User).WithMany(p => p.ReadingProgresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("ReadingProgress_ibfk_2");
        });

        modelBuilder.Entity<Recomendation>(entity =>
        {
            entity.HasKey(e => e.RecId).HasName("PRIMARY");

            entity.HasIndex(e => e.BookId, "BookId");

            entity.HasIndex(e => e.UserId, "idx_UserId");

            entity.HasIndex(e => e.GeneratedDate, "inx_GeneratedDate");

            entity.Property(e => e.RecId).HasColumnType("int(11)");
            entity.Property(e => e.BookId).HasColumnType("int(11)");
            entity.Property(e => e.GeneratedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.IsViewed).HasDefaultValueSql("'0'");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasComment("AI explanation");
            entity.Property(e => e.Score)
                .HasPrecision(4, 3)
                .HasComment("0.000-1.000 similarity");
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.Book).WithMany(p => p.Recomendations)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("Recomendations_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.Recomendations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Recomendations_ibfk_1");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PRIMARY");

            entity.HasIndex(e => e.LibrarianId, "LibrarianId");

            entity.HasIndex(e => e.BorrowDate, "idx_BorrowDate");

            entity.HasIndex(e => e.CopyId, "idx_CopyId");

            // entity.Property(e => e.UserId).HasMaxLength(450);

            // entity.HasIndex(e => e.Status, "idx_Status");

            entity.Property(e => e.TransactionId).HasColumnType("int(11)");
            entity.Property(e => e.BorrowDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.CopyId).HasColumnType("int(11)");
            entity.Property(e => e.DueDate).HasColumnType("datetime");
            // entity.Property(e => e.LibrarianId).HasMaxLength(450);
            // entity.Property(e => e.UserId).HasColumnType("int(11)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ReturnDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasConversion<int>() // đảm bảo lưu int
                .HasDefaultValue(TransactionStatus.Borrowed);

            entity.HasOne(d => d.Copy).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.CopyId)
                .HasConstraintName("Transactions_ibfk_2");

            entity.HasOne(d => d.Librarian).WithMany(p => p.ManagedTransactions)
                .HasForeignKey(d => d.LibrarianId)
                .HasConstraintName("Transactions_ibfk_3");

            entity.HasOne(d => d.User).WithMany(p => p.BorrowTransactions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("Transactions_ibfk_1");
        });

        modelBuilder.Entity<UserFavoriteBook>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.BookId, "BookId");

            entity.HasIndex(e => e.UserId, "idx_UserId");

            entity.HasIndex(e => new { e.UserId, e.BookId }, "unique_user_book").IsUnique();

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.BookId).HasColumnType("int(11)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.Book).WithMany(p => p.UserFavoriteBooks)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("UserFavoriteBooks_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.UserFavoriteBooks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("UserFavoriteBooks_ibfk_1");
        });

        modelBuilder.Entity<UserReadingHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PRIMARY");

            entity.ToTable("UserReadingHistory");

            entity.HasIndex(e => e.BookId, "BookId");

            entity.HasIndex(e => e.CreatedAt, "idx_CreatedAt");

            entity.HasIndex(e => e.UserId, "idx_UserId");

            entity.Property(e => e.HistoryId).HasColumnType("int(11)");
            entity.Property(e => e.Action)
                .HasMaxLength(20)
                .HasComment("Read/Borrowed/Liked/Rated");
            entity.Property(e => e.BookId).HasColumnType("int(11)");
            entity.Property(e => e.DurationMinutes)
                .HasDefaultValueSql("'0'")
                .HasColumnType("int(11)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.Book).WithMany(p => p.UserReadingHistories)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("UserReadingHistory_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.UserReadingHistories)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("UserReadingHistory_ibfk_1");
        });

        modelBuilder.Entity<vw_BookAvailability>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_BookAvailability");

            entity.Property(e => e.AvailableCopies).HasColumnType("bigint(21)");
            entity.Property(e => e.AvailableLocations).HasColumnType("text");
            entity.Property(e => e.BookId).HasColumnType("int(11)");
            entity.Property(e => e.BookLocation).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.TotalCopies)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)");
        });

        modelBuilder.Entity<vw_StudentStat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_StudentStats");

            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Id).HasMaxLength(450);
            entity.Property(e => e.LibraryStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'Active'")
                .HasComment("LIBRARY: Active/Suspended/Expired/Blacklisted");
            entity.Property(e => e.StudentClass).HasMaxLength(20);
            entity.Property(e => e.TotalBorrows).HasColumnType("bigint(21)");
            entity.Property(e => e.TotalFines).HasPrecision(32, 2);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
