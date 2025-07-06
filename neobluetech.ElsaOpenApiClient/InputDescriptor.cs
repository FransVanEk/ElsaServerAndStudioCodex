// CustomActivity/OpenApiSpecificationParser.cs
namespace CustomActivity;

// Supporting classes for the descriptor
public class InputDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsRequired { get; set; }
    public List<SelectListItem>? Options { get; set; }
}
