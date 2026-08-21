public class DDCTreeDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public List<DDCTreeDto> Children { get; set; } = new();
}
public class CreateDDCDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? ParentCode { get; set; }
}