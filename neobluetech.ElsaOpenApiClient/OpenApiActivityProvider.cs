// CustomActivity/OpenApiSpecificationParser.cs
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace CustomActivity;

public class OpenApiActivityProvider : IActivityProvider
{
    private readonly IOpenApiSpecificationParser _parser;
    private readonly ILogger<OpenApiActivityProvider> _logger;
    private readonly ILogger<OpenApiActivity> _activitylogger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _specificationsPath;
    private List<OpenApiSpecification> _specifications = new();

    public OpenApiActivityProvider(
       IOpenApiSpecificationParser parser,
       ILogger<OpenApiActivityProvider> logger,
       ILogger<OpenApiActivity> activitylogger,
       IHttpClientFactory httpClientFactory, // Add this parameter
       string specificationsPath = "OpenApiSpecs")
    {
        _parser = parser;
        _logger = logger;
        _activitylogger = activitylogger;
        _httpClientFactory = httpClientFactory;
        _specificationsPath = specificationsPath;
    }

    public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _specifications = await _parser.DiscoverSpecificationsAsync(_specificationsPath);
            var descriptors = new List<ActivityDescriptor>();

            foreach (var spec in _specifications)
            {
                var typeName = $"OpenApi_{spec.Title?.Replace(" ", "_").Replace("-", "_") ?? "Unknown"}";

                var descriptor = new ActivityDescriptor
                {
                    TypeName = $"CustomActivity.{typeName}",
                    Namespace = "CustomActivity",
                    Name = typeName,
                    Version = 1,
                    DisplayName = $"OpenAPI: {spec.Title}",
                    Description = $"Execute endpoints from {spec.Title} API (v{spec.Version})",
                    Category = "OpenAPI",
                    Kind = ActivityKind.Task,
                    IsBrowsable = true,
                    Constructor = _ => new OpenApiActivity(_httpClientFactory, _activitylogger, spec)
                };

                descriptors.Add(descriptor);
            }

            _logger.LogInformation("Registered {Count} OpenAPI activities", descriptors.Count);
            return descriptors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading OpenAPI activity descriptors");
            return Enumerable.Empty<ActivityDescriptor>();
        }
    }
}
