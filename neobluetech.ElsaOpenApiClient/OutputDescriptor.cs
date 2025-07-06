// CustomActivity/OpenApiSpecificationParser.cs
namespace CustomActivity;

public class OutputDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
