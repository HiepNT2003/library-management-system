using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class UserFavoriteBook
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public int? BookId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Book? Book { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}
