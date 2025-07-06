// CustomActivity/OpenApiSpecificationParser.cs
namespace CustomActivity;

public interface IOpenApiSpecificationParser
{
    Task<OpenApiSpecification> ParseAsync(string filePath);
    Task<List<OpenApiSpecification>> DiscoverSpecificationsAsync(string directoryPath);
}
