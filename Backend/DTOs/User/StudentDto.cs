public class CreateStudentRequest
{
    public string FullName { get; set; }
    public string Faculty { get; set; }
    public string Class { get; set; }
    public string? Email { get; set; }

    public string StudentCode { get; set; }
    public string Term { get; set; }
    public string? PhoneNumber { get; set; }
    public UserStatus Status { get; set; }
    public DateTime? ExpiredDate { get; set; }
}