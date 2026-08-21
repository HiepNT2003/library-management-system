using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Backend.Models;

public class BorrowPolicy
{
    public int Id { get; set; }

    public string AspNetRoleId { get; set; }
    public int DocumentTypeId { get; set; }

    public int MaxBorrowDays { get; set; }
    public int MaxBooks { get; set; }
    public int MaxExtention { get; set; } // extend borrow day

    public decimal FinePerDay { get; set; }
    public int FinePaymentDeadlineDays { get; set; } = 7;

    public IdentityRole Role { get; set; }
    public DocumentType DocumentType { get; set; }
}