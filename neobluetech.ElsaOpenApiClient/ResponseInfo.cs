// CustomActivity/ResponseInfo.cs
namespace CustomActivity;

public class ResponseInfo
{
    public string? Description { get; set; }
    public Dictionary<string, MediaTypeInfo> Content { get; set; } = new();
}