using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Backend.Models;
using Backend.Services;
public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
    Task SendFineCreatedAsync(string toEmail, string userName, string bookTitle, decimal amount, string reason);
    Task SendBorrowApprovedAsync(string toEmail, string userName, string bookTitle, DateTime dueDate, int requestId);
    Task SendBorrowRejectedAsync(string toEmail, string userName, string bookTitle, string reason);
    Task SendDueSoonAsync(string toEmail, string userName, string bookTitle, DateTime dueDate);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl   = true,
            Credentials = new NetworkCredential(_settings.SenderEmail, _settings.Password)
        };

        var mail = new MailMessage
        {
            From       = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true
        };
        mail.To.Add(toEmail);

        await client.SendMailAsync(mail);
    }

    public async Task SendFineCreatedAsync(string toEmail, string userName, string bookTitle, decimal amount, string reason)
    {
        var subject = "Thông báo phiếu phạt thư viện";
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
              <h2 style="color: #c62828;">Thông báo phiếu phạt</h2>
              <p>Xin chào <strong>{userName}</strong>,</p>
              <p>Bạn có phiếu phạt từ thư viện ĐH Giao thông Vận tải:</p>
              <table style="width:100%; border-collapse:collapse; margin: 16px 0;">
                <tr style="background:#f5f5f5;">
                  <td style="padding:10px; border:1px solid #ddd;">Sách</td>
                  <td style="padding:10px; border:1px solid #ddd;"><strong>{bookTitle}</strong></td>
                </tr>
                <tr>
                  <td style="padding:10px; border:1px solid #ddd;">Lý do</td>
                  <td style="padding:10px; border:1px solid #ddd;">{reason}</td>
                </tr>
                <tr style="background:#fff3e0;">
                  <td style="padding:10px; border:1px solid #ddd;">Số tiền phạt</td>
                  <td style="padding:10px; border:1px solid #ddd; color:#c62828;">
                    <strong>{amount:N0} VNĐ</strong>
                  </td>
                </tr>
              </table>
              <p>Vui lòng thanh toán tại quầy thư viện.</p>
              <p style="color:#888; font-size:12px;">Thư viện ĐH Giao thông Vận tải</p>
            </div>
        """;
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendBorrowApprovedAsync(string toEmail, string userName, string bookTitle, DateTime dueDate, int requestId)
    {
        var subject = "Yêu cầu mượn sách đã được duyệt";
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
              <h2 style="color: #2e7d32;">Yêu cầu mượn sách đã được duyệt ✓</h2>
              <p>Xin chào <strong>{userName}</strong>,</p>
              <p>Yêu cầu mượn sách của bạn đã được duyệt:</p>
              <table style="width:100%; border-collapse:collapse; margin: 16px 0;">
                <tr style="background:#f5f5f5;">
                  <td style="padding:10px; border:1px solid #ddd;">Sách</td>
                  <td style="padding:10px; border:1px solid #ddd;"><strong>{bookTitle}</strong></td>
                </tr>
                <tr>
                  <td style="padding:10px; border:1px solid #ddd;">Hạn lấy sách</td>
                  <td style="padding:10px; border:1px solid #ddd;"><strong>{dueDate:dd/MM/yyyy}</strong></td>
                </tr>
                <tr style="background:#e8f5e9;">
                  <td style="padding:10px; border:1px solid #ddd;">Mã yêu cầu</td>
                  <td style="padding:10px; border:1px solid #ddd;">
                    <strong style="font-size:20px; font-family:monospace;">#{requestId}</strong>
                  </td>
                </tr>
              </table>
              <p>Mang mã yêu cầu <strong>#{requestId}</strong> đến thư viện để lấy sách.</p>
              <p>Vui lòng đến thư viện để lấy sách trước hạn trên.</p>
              <p style="color:#888; font-size:12px;">Thư viện ĐH Giao thông Vận tải</p>
            </div>
        """;
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendBorrowRejectedAsync(string toEmail, string userName, string bookTitle, string reason)
    {
        var subject = "Yêu cầu mượn sách không được duyệt";
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
              <h2 style="color: #c62828;">Yêu cầu mượn sách không được duyệt</h2>
              <p>Xin chào <strong>{userName}</strong>,</p>
              <p>Rất tiếc, yêu cầu mượn sách của bạn không được duyệt:</p>
              <table style="width:100%; border-collapse:collapse; margin: 16px 0;">
                <tr style="background:#f5f5f5;">
                  <td style="padding:10px; border:1px solid #ddd;">Sách</td>
                  <td style="padding:10px; border:1px solid #ddd;"><strong>{bookTitle}</strong></td>
                </tr>
                <tr>
                  <td style="padding:10px; border:1px solid #ddd;">Lý do</td>
                  <td style="padding:10px; border:1px solid #ddd;">{reason ?? "Không có lý do cụ thể"}</td>
                </tr>
              </table>
              <p style="color:#888; font-size:12px;">Thư viện ĐH Giao thông Vận tải</p>
            </div>
        """;
        await SendAsync(toEmail, subject, body);
    }

    public async Task SendDueSoonAsync(string toEmail, string userName, string bookTitle, DateTime dueDate)
    {
        var daysLeft = (dueDate - DateTime.Now).Days;
        var subject  = $"Nhắc nhở: Sách sắp đến hạn trả ({daysLeft} ngày)";
        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
              <h2 style="color: #e65100;">⏰ Nhắc nhở trả sách</h2>
              <p>Xin chào <strong>{userName}</strong>,</p>
              <p>Sách bạn đang mượn sắp đến hạn trả:</p>
              <table style="width:100%; border-collapse:collapse; margin: 16px 0;">
                <tr style="background:#f5f5f5;">
                  <td style="padding:10px; border:1px solid #ddd;">Sách</td>
                  <td style="padding:10px; border:1px solid #ddd;"><strong>{bookTitle}</strong></td>
                </tr>
                <tr style="background:#fff3e0;">
                  <td style="padding:10px; border:1px solid #ddd;">Hạn trả</td>
                  <td style="padding:10px; border:1px solid #ddd; color:#e65100;">
                    <strong>{dueDate:dd/MM/yyyy} (còn {daysLeft} ngày)</strong>
                  </td>
                </tr>
              </table>
              <p>Vui lòng trả sách đúng hạn để tránh bị phạt.</p>
              <p style="color:#888; font-size:12px;">Thư viện ĐH Giao thông Vận tải</p>
            </div>
        """;
        await SendAsync(toEmail, subject, body);
    }
}