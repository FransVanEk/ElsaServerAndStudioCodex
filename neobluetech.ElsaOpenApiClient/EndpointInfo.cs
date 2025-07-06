// CustomActivity/EndpointInfo.cs
namespace CustomActivity;

public class EndpointInfo
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? OperationId { get; set; }
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public List<ParameterInfo> Parameters { get; set; } = new();
    public RequestBodyInfo? RequestBody { get; set; }
    public Dictionary<string, ResponseInfo> Responses { get; set; } = new();
}
