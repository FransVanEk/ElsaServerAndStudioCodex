// CustomActivity/RequestBodyInfo.cs
namespace CustomActivity;

public class RequestBodyInfo
{
    public string? Description { get; set; }
    public bool Required { get; set; }
    public Dictionary<string, MediaTypeInfo> Content { get; set; } = new();
}
