// CustomActivity/ParameterInfo.cs
namespace CustomActivity;

public class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string In { get; set; } = string.Empty; // query, path, header
    public string? Description { get; set; }
    public bool Required { get; set; }
    public string? Type { get; set; }
}
