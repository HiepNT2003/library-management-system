using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class StaffProfile
{
    public string UserId { get; set; }
    public ApplicationUser User { get; set; }
    public string StaffCode { get; set; } 
    public string? Department { get; set; } // Khoa/Phòng ban
    public string? Position { get; set; }
}