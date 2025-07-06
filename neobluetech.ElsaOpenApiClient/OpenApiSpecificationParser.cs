// CustomActivity/OpenApiSpecificationParser.cs
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CustomActivity;

public class OpenApiSpecificationParser : IOpenApiSpecificationParser
{
    private readonly ILogger<OpenApiSpecificationParser> _logger;


    public OpenApiSpecificationParser(ILogger<OpenApiSpecificationParser> logger)
    {
        _logger = logger;
    }

    public async Task<OpenApiSpecification> ParseAsync(string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var document = JsonDocument.Parse(content);

            var specification = new OpenApiSpecification
            {
                Title = document.RootElement.GetProperty("info").GetProperty("title").GetString(),
                Version = document.RootElement.GetProperty("info").GetProperty("version").GetString()
            };

            // Parse servers for base URL
            if (document.RootElement.TryGetProperty("servers", out var serversElement) && serversElement.ValueKind == JsonValueKind.Array)
            {
                var firstServer = serversElement.EnumerateArray().FirstOrDefault();
                if (firstServer.ValueKind != JsonValueKind.Undefined)
                {
                    specification.BaseUrl = firstServer.GetProperty("url").GetString();
                }
            }

            // Parse paths
            if (document.RootElement.TryGetProperty("paths", out var pathsElement))
            {
                foreach (var pathProperty in pathsElement.EnumerateObject())
                {
                    var path = pathProperty.Name;
                    var pathItem = pathProperty.Value;

                    foreach (var methodProperty in pathItem.EnumerateObject())
                    {
                        var method = methodProperty.Name.ToLower();
                        if (IsValidHttpMethod(method))
                        {
                            var operation = methodProperty.Value;
                            var endpoint = ParseEndpoint(path, method, operation);
                            specification.Endpoints.Add(endpoint);
                        }
                    }
                }
            }

            return specification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing OpenAPI specification from {FilePath}", filePath);
            throw;
        }
    }

    public async Task<List<OpenApiSpecification>> DiscoverSpecificationsAsync(string directoryPath)
    {
        var specifications = new List<OpenApiSpecification>();

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("OpenAPI specifications directory not found: {DirectoryPath}", directoryPath);
            return specifications;
        }

        var jsonFiles = Directory.GetFiles(directoryPath, "*.json", SearchOption.AllDirectories);

        foreach (var file in jsonFiles)
        {
            try
            {
                var spec = await ParseAsync(file);
                specifications.Add(spec);
                _logger.LogInformation("Successfully parsed OpenAPI specification: {Title}", spec.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse OpenAPI specification from {File}", file);
            }
        }

        return specifications;
    }

    private EndpointInfo ParseEndpoint(string path, string method, JsonElement operation)
    {
        var endpoint = new EndpointInfo
        {
            Path = path,
            Method = method,
            OperationId = operation.TryGetProperty("operationId", out var opId) ? opId.GetString() : null,
            Summary = operation.TryGetProperty("summary", out var summary) ? summary.GetString() : null,
            Description = operation.TryGetProperty("description", out var desc) ? desc.GetString() : null
        };

        // Parse parameters
        if (operation.TryGetProperty("parameters", out var parameters) && parameters.ValueKind == JsonValueKind.Array)
        {
            foreach (var param in parameters.EnumerateArray())
            {
                var paramInfo = new ParameterInfo
                {
                    Name = param.GetProperty("name").GetString() ?? string.Empty,
                    In = param.GetProperty("in").GetString() ?? string.Empty,
                    Description = param.TryGetProperty("description", out var paramDesc) ? paramDesc.GetString() : null,
                    Required = param.TryGetProperty("required", out var required) && required.GetBoolean()
                };

                if (param.TryGetProperty("schema", out var schema) && schema.TryGetProperty("type", out var type))
                {
                    paramInfo.Type = type.GetString();
                }

                endpoint.Parameters.Add(paramInfo);
            }
        }

        // Parse request body
        if (operation.TryGetProperty("requestBody", out var requestBody))
        {
            var requestBodyInfo = new RequestBodyInfo
            {
                Description = requestBody.TryGetProperty("description", out var rbDesc) ? rbDesc.GetString() : null,
                Required = requestBody.TryGetProperty("required", out var rbRequired) && rbRequired.GetBoolean()
            };

            if (requestBody.TryGetProperty("content", out var content))
            {
                foreach (var contentType in content.EnumerateObject())
                {
                    requestBodyInfo.Content[contentType.Name] = new MediaTypeInfo
                    {
                        Schema = contentType.Value.TryGetProperty("schema", out var schema) ? schema : null
                    };
                }
            }

            endpoint.RequestBody = requestBodyInfo;
        }

        // Parse responses
        if (operation.TryGetProperty("responses", out var responses))
        {
            foreach (var response in responses.EnumerateObject())
            {
                var responseInfo = new ResponseInfo
                {
                    Description = response.Value.TryGetProperty("description", out var respDesc) ? respDesc.GetString() : null
                };

                if (response.Value.TryGetProperty("content", out var respContent))
                {
                    foreach (var contentType in respContent.EnumerateObject())
                    {
                        responseInfo.Content[contentType.Name] = new MediaTypeInfo
                        {
                            Schema = contentType.Value.TryGetProperty("schema", out var schema) ? schema : null
                        };
                    }
                }

                endpoint.Responses[response.Name] = responseInfo;
            }
        }

        return endpoint;
    }

    private bool IsValidHttpMethod(string method)
    {
        return method switch
        {
            "get" or "post" or "put" or "delete" or "patch" or "head" or "options" => true,
            _ => false
        };
    }

    
  
}
