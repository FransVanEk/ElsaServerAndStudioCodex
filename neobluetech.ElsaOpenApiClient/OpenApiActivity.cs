// CustomActivity/OpenApiActivity.cs
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace CustomActivity;

/// <summary>
/// Execute an OpenAPI endpoint
/// </summary>
[Activity("OpenApi", "OpenAPI", "Execute an OpenAPI endpoint", Kind = ActivityKind.Task)]
public class OpenApiActivity : CodeActivity
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenApiActivity> _logger;
    private readonly OpenApiSpecification _specification;

    public OpenApiActivity(
        IHttpClientFactory httpClientFactory,
        ILogger<OpenApiActivity> logger,
        OpenApiSpecification specification)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _specification = specification;
    }

    /// <summary>
    /// The endpoint to call
    /// </summary>
    [Input(
        DisplayName = "Endpoint",
        Description = "Select the API endpoint to call",
        Category = "Settings"
    )]
    public Input<string> Endpoint { get; set; } = default!;

    /// <summary>
    /// Additional headers to include in the request
    /// </summary>
    [Input(
        DisplayName = "Headers",
        Description = "Additional headers to include in the request",
        Category = "Settings"
    )]
    public Input<IDictionary<string, string>?> Headers { get; set; } = default!;

    /// <summary>
    /// Query parameters for the request
    /// </summary>
    [Input(
        DisplayName = "Query Parameters",
        Description = "Query parameters for the request",
        Category = "Settings"
    )]
    public Input<IDictionary<string, string>?> QueryParameters { get; set; } = default!;

    /// <summary>
    /// Request body (for POST/PUT requests)
    /// </summary>
    [Input(
        DisplayName = "Request Body",
        Description = "Request body (for POST/PUT requests)",
        Category = "Settings"
    )]
    public Input<object?> RequestBody { get; set; } = default!;

    /// <summary>
    /// Path parameters for the endpoint
    /// </summary>
    [Input(
        DisplayName = "Path Parameters",
        Description = "Path parameters for the endpoint",
        Category = "Settings"
    )]
    public Input<IDictionary<string, string>?> PathParameters { get; set; } = default!;

    /// <summary>
    /// The response from the API call
    /// </summary>
    [Output(
        DisplayName = "Response",
        Description = "The response from the API call"
    )]
    public Output<object?> Response { get; set; } = default!;

    /// <summary>
    /// HTTP status code of the response
    /// </summary>
    [Output(
        DisplayName = "Status Code",
        Description = "HTTP status code of the response"
    )]
    public Output<int> StatusCode { get; set; } = default!;

    /// <summary>
    /// Response headers
    /// </summary>
    [Output(
        DisplayName = "Response Headers",
        Description = "Response headers"
    )]
    public Output<IDictionary<string, IEnumerable<string>>?> ResponseHeaders { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var endpoint = Endpoint.Get(context);
        var headers = Headers.GetOrDefault(context);
        var queryParameters = QueryParameters.GetOrDefault(context);
        var requestBody = RequestBody.GetOrDefault(context);
        var pathParameters = PathParameters.GetOrDefault(context);

        try
        {
            if (string.IsNullOrEmpty(endpoint))
            {
                _logger.LogError("No endpoint specified for OpenAPI activity");
                await context.CompleteActivityWithOutcomesAsync("Error");
                return;
            }

            var endpointInfo = _specification.GetEndpoint(endpoint);
            if (endpointInfo == null)
            {
                _logger.LogError("Endpoint '{Endpoint}' not found in OpenAPI specification", endpoint);
                await context.CompleteActivityWithOutcomesAsync("Error");
                return;
            }

            var httpClient = _httpClientFactory.CreateClient();

            // Set base URL
            if (!string.IsNullOrEmpty(_specification.BaseUrl))
            {
                httpClient.BaseAddress = new Uri(_specification.BaseUrl);
            }

            // Build the request
            var request = BuildHttpRequest(endpointInfo, headers, queryParameters, requestBody, pathParameters);

            // Execute the request
            var response = await httpClient.SendAsync(request, context.CancellationToken);

            // Process the response
            var responseContent = await response.Content.ReadAsStringAsync();

            var responseObject = string.IsNullOrEmpty(responseContent) ? null : JsonSerializer.Deserialize<object>(responseContent);
            var statusCode = (int)response.StatusCode;
            var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => h.Value);

            // Set outputs
            Response.Set(context, responseObject);
            StatusCode.Set(context, statusCode);
            ResponseHeaders.Set(context, responseHeaders);

            _logger.LogInformation("OpenAPI call to {Endpoint} completed with status {StatusCode}", endpoint, statusCode);

            await context.CompleteActivityWithOutcomesAsync("Done");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing OpenAPI activity for endpoint {Endpoint}", endpoint);
            await context.CompleteActivityWithOutcomesAsync("Error");
        }
    }

    private HttpRequestMessage BuildHttpRequest(
        EndpointInfo endpointInfo,
        IDictionary<string, string>? headers,
        IDictionary<string, string>? queryParameters,
        object? requestBody,
        IDictionary<string, string>? pathParameters)
    {
        var path = endpointInfo.Path;

        // Replace path parameters
        if (pathParameters != null)
        {
            foreach (var param in pathParameters)
            {
                path = path.Replace($"{{{param.Key}}}", param.Value);
            }
        }

        // Add query parameters
        if (queryParameters != null && queryParameters.Any())
        {
            var queryString = string.Join("&", queryParameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            path += $"?{queryString}";
        }

        var request = new HttpRequestMessage(new HttpMethod(endpointInfo.Method), path);

        // Add headers
        if (headers != null)
        {
            foreach (var header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Add request body for POST/PUT/PATCH
        if (requestBody != null && (endpointInfo.Method.ToUpper() == "POST" || endpointInfo.Method.ToUpper() == "PUT" || endpointInfo.Method.ToUpper() == "PATCH"))
        {
            var json = JsonSerializer.Serialize(requestBody);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }
}
