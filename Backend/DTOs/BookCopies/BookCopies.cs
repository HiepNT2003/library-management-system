public class BookCopyDto
{
    public int CopyId { get; set; }
    public int? BookId { get; set; }
    public string? Barcode { get; set; }
    public string Status { get; set; } = "Available";
    public string? ShelfLocation { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string? BookCondition { get; set; }
    public bool IsReferenceOnly { get; set; }
    public string? Notes { get; set; }
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}
 
public class CreateBookCopyDto
{
    public int BookId { get; set; }
    public string? Barcode { get; set; }
    public string? ShelfLocation { get; set; }
    public string? BookCondition { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public bool IsReferenceOnly { get; set; }
    public string? Notes { get; set; }
    public int? WarehouseId { get; set; }
}
 
public class CreateBulkBookCopyDto
{
    public int BookId { get; set; }
    public int Quantity { get; set; }
    public string? ShelfLocation { get; set; }
    public string? BookCondition { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public bool IsReferenceOnly { get; set; }
    public int? WarehouseId { get; set; }
}
 
public class UpdateBookCopyDto
{
    public string? Barcode { get; set; }
    public string? ShelfLocation { get; set; }
    public string? BookCondition { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public bool IsReferenceOnly { get; set; }
    public string? Notes { get; set; }
    public int? WarehouseId { get; set; }
}

public class BookCopyAllDto
{
    public int CopyId { get; set; }
    public int? BookId { get; set; }
    public string? BookTitle { get; set; }
    public string? Barcode { get; set; }
    public string Status { get; set; } = "Available";
    public string? ShelfLocation { get; set; }
    public string? BookCondition { get; set; }
    public bool IsReferenceOnly { get; set; }
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string? Notes { get; set; }
}
 
public class UpdateStatusDto
{
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
}

public class ImportBookCopyDto
{
    public int BookId { get; set; }
    public string? Barcode { get; set; }
    public int WarehouseId { get; set; }
    public string? ShelfLocation { get; set; }
    public string? BookCondition { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public bool IsReferenceOnly { get; set; }
    public string? Notes { get; set; }
}