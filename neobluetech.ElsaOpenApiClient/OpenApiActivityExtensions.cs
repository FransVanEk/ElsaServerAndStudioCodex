// CustomActivity/OpenApiActivityExtensions.cs
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CustomActivity;

public static class OpenApiActivityExtensions
{
    public static IServiceCollection AddOpenApiActivities(this IServiceCollection services, string? specificationsPath = null)
    {
        // Set default path if not provided
        specificationsPath ??= Path.Combine(Directory.GetCurrentDirectory(), "OpenApiSpecs");

        // Register the parser
        services.AddSingleton<IOpenApiSpecificationParser, OpenApiSpecificationParser>();

        // Register the activity provider
        services.AddSingleton<IActivityProvider>(serviceProvider =>
        {
            var parser = serviceProvider.GetRequiredService<IOpenApiSpecificationParser>();
            var logger = serviceProvider.GetRequiredService<ILogger<OpenApiActivityProvider>>();
            var activityLogger = serviceProvider.GetRequiredService<ILogger<OpenApiActivity>>();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            return new OpenApiActivityProvider(parser, logger, activityLogger,httpClientFactory, specificationsPath);
        });

        // Ensure HttpClient is available
        services.AddHttpClient();

        return services;
    }
}

