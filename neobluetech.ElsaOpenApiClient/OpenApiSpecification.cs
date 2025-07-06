// CustomActivity/OpenApiSpecification.cs
namespace CustomActivity;

// Supporting classes
public class OpenApiSpecification
{
    public string? BaseUrl { get; set; }
    public string? Title { get; set; }
    public string? Version { get; set; }
    public List<EndpointInfo> Endpoints { get; set; } = new();

    public EndpointInfo? GetEndpoint(string endpointId)
    {
        return Endpoints.FirstOrDefault(e => e.OperationId == endpointId || $"{e.Method}_{e.Path}" == endpointId);
    }

    public List<EndpointInfo> GetEndpoints()
    {
        return Endpoints;
    }
}
