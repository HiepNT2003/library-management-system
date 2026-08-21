using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class DailyLibraryJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyLibraryJob> _logger;
    private readonly IRecommendationService _recommendationService;

    public DailyLibraryJob(IServiceProvider serviceProvider, ILogger<DailyLibraryJob> logger, IRecommendationService recommendationService)
    {
        _serviceProvider = serviceProvider;
        _logger          = logger;
        _recommendationService = recommendationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyLibraryJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Tính thời gian đến 00:01 ngày mai
                var now      = DateTime.Now;
                var nextRun  = now.Date.AddDays(1).AddMinutes(1);
                var delay    = nextRun - now;

                _logger.LogInformation("Next job run at: {NextRun}", nextRun);
                await Task.Delay(delay, stoppingToken);

                await RunJobs(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DailyLibraryJob");
                // Nếu lỗi thì đợi 5 phút rồi thử lại
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("DailyLibraryJob stopped.");
    }

    private async Task RunJobs(CancellationToken stoppingToken)
    {
        // Dùng scope riêng vì DbContext là scoped service
        using var scope        = _serviceProvider.CreateScope();
        var context            = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService       = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        
        await SendDueSoonNotifications(context, notificationService, stoppingToken);
        await SendOverdueNotifications(context, notificationService);
        await UpdateOverdueTransactions(context);
        await SendDueSoonEmails(context, emailService, stoppingToken);
        await RemindLibrarianPendingRequests(context, notificationService);
        await CancelExpiredRequests(context, notificationService);
        await _recommendationService.TrainAsync();
    }

    // ---- Job 1: Cập nhật Overdue ----
    private async Task UpdateOverdueTransactions(AppDbContext context)
    {
        try
        {
            var updated = await context.Transactions
                .Where(t => t.Status == TransactionStatus.Borrowed && t.DueDate < DateTime.Now)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, TransactionStatus.Overdue));

            if (updated > 0)
                _logger.LogInformation("Updated {Count} transactions to Overdue.", updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating overdue transactions.");
        }
    }

    // ---- Job 2: Gửi email nhắc trả sách ----
    private async Task SendDueSoonEmails(AppDbContext context, IEmailService emailService, CancellationToken stoppingToken)
    {
        try
        {
            // Tìm transaction đến hạn trong 3 ngày tới
            var today       = DateTime.Today;
            var threeDaysLater = today.AddDays(3);

            var transactions = await context.Transactions
                .Include(t => t.User)
                .Include(t => t.Copy)
                    .ThenInclude(c => c.Book)
                .Where(t =>
                    t.Status == TransactionStatus.Borrowed &&
                    t.DueDate.Date >= today &&
                    t.DueDate.Date <= threeDaysLater &&
                    t.User != null &&
                    t.User.Email != null)
                .ToListAsync(stoppingToken);

            _logger.LogInformation("Sending due-soon emails for {Count} transactions.", transactions.Count);

            foreach (var tx in transactions)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    await emailService.SendDueSoonAsync(
                        tx.User!.Email!,
                        tx.User.FullName ?? "",
                        tx.Copy?.Book?.Title ?? "",
                        tx.DueDate
                    );

                    // Đợi 200ms giữa các email tránh spam
                    await Task.Delay(200, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send due-soon email to {Email}", tx.User?.Email);
                }
            }

            _logger.LogInformation("Due-soon emails sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending due-soon emails.");
        }
    }

    private async Task CancelExpiredRequests(AppDbContext context, INotificationService notifService)
    {
        var today = DateTime.Today;

        var expiredRequests = await context.BorrowRequests
            .Include(r => r.Book)
            .Where(r => (r.Status == RequestStatus.Pending ||
                        r.Status == RequestStatus.Approved) &&
                        r.ExpectedBorrowDate.HasValue &&
                        r.ExpectedBorrowDate.Value.Date < today)
            .ToListAsync();

        if (!expiredRequests.Any()) return;

        foreach (var req in expiredRequests)
        {
            var originalStatus = req.Status;
            req.Status = RequestStatus.Cancelled;

            var reason = originalStatus == RequestStatus.Pending
                ? "chưa được duyệt kịp"
                : "bạn không đến lấy sách";

            await notifService.CreateAsync(
                req.UserId,
                "Yêu cầu mượn sách đã bị huỷ",
                NotificationType.BorrowRejected,
                $"Yêu cầu mượn \"{req.Book?.Title}\" đã bị huỷ do quá ngày dự kiến ({req.ExpectedBorrowDate!.Value:dd/MM/yyyy}). Lý do: {reason}.",
                "/my-requests"
            );
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("Cancelled {Count} expired requests.", expiredRequests.Count);
    }

    private async Task RemindLibrarianPendingRequests(AppDbContext context, INotificationService notifService)
    {
        var tomorrow = DateTime.Today.AddDays(1);

        // Tìm request Pending sẽ hết hạn ngày mai
        var soonExpired = await context.BorrowRequests
            .Include(r => r.Book)
            .Where(r => r.Status == RequestStatus.Pending &&
                        r.ExpectedBorrowDate.HasValue &&
                        r.ExpectedBorrowDate.Value.Date == tomorrow)
            .ToListAsync();

        if (!soonExpired.Any()) return;

        await notifService.CreateForRoleAsync(
            "Librarian",
            "Có yêu cầu mượn sắp hết hạn",
            NotificationType.NewRequest,
            $"Có {soonExpired.Count} yêu cầu mượn sẽ hết hạn vào ngày mai. Vui lòng xử lý sớm.",
            "/admin/borrow-requests"
        );

        _logger.LogInformation("Reminded librarians: {Count} pending requests expire tomorrow.", soonExpired.Count);
    }

    private async Task SendDueSoonNotifications(AppDbContext context, INotificationService notifService, CancellationToken ct)
    {
        var threeDaysLater = DateTime.Today.AddDays(3);
        var transactions   = await context.Transactions
            .Include(t => t.Copy).ThenInclude(c => c.Book)
            .Where(t => t.Status == TransactionStatus.Borrowed &&
                        t.DueDate.Date <= threeDaysLater &&
                        t.DueDate.Date >= DateTime.Today)
            .ToListAsync(ct);

        foreach (var tx in transactions)
        {
            if (ct.IsCancellationRequested) break;
            var daysLeft = (tx.DueDate.Date - DateTime.Today).Days;
            await notifService.CreateAsync(
                tx.UserId,
                "Sắp đến hạn trả sách",
                NotificationType.DueSoon,
                $"\"{tx.Copy?.Book?.Title}\" sẽ đến hạn sau {daysLeft} ngày ({tx.DueDate:dd/MM/yyyy})",
                "/my-books"
            );
        }
    }

    private async Task SendOverdueNotifications(AppDbContext context, INotificationService notifService)
    {
        var overdues = await context.Transactions
            .Include(t => t.Copy).ThenInclude(c => c.Book)
            .Where(t => t.Status == TransactionStatus.Overdue)
            .ToListAsync();

        foreach (var tx in overdues)
        {
            var days = (int)(DateTime.Now - tx.DueDate).TotalDays;
            await notifService.CreateAsync(
                tx.UserId,
                "Sách quá hạn trả",
                NotificationType.Overdue,
                $"\"{tx.Copy?.Book?.Title}\" đã quá hạn {days} ngày. Vui lòng đến thư viện ngay.",
                "/my-books"
            );
        }
    }
}