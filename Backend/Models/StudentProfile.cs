using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

public class StudentProfile
{
    public string UserId { get; set; } = null;
    public ApplicationUser User { get; set; }
    public string StudentCode { get; set; } 
    public string? Class { get; set; }
    public string? Faculty { get; set; }
    public string? Major { get; set; }
    public string? Term { get; set; } // Khóa (K60, K61...) 
    public int? AdmissionYear { get; set; }
}