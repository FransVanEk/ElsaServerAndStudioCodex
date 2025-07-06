using CustomActivity;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Workflows;
using Microsoft.AspNetCore.Mvc;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
var services = builder.Services;
var configuration = builder.Configuration;
services
    .AddElsa(elsa =>
    {
        elsa
            .UseIdentity(identity =>
            {
                identity.TokenOptions = options => options.SigningKey = "large-signing-key-for-signing-JWT-tokens";
                identity.UseAdminUserProvider();
            })
            .UseDefaultAuthentication()
            .UseWorkflowManagement(management =>
            {
                management.UseEntityFrameworkCore(ef => ef.UseSqlite());
            })
        .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseScheduling()
        .UseJavaScript()
        .UseLiquid()
        .UseCSharp()
        .UseHttp(http => http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options))
        .UseWorkflowsApi()
        .AddActivitiesFrom<Program>()
        .AddWorkflowsFrom<Program>();
    });

// Register OpenAPI services separately
services.AddSingleton<IOpenApiSpecificationParser, OpenApiSpecificationParser>();
services.AddSingleton<IActivityProvider, OpenApiActivityProvider>(serviceProvider =>
{
    var parser = serviceProvider.GetRequiredService<IOpenApiSpecificationParser>();
    var logger = serviceProvider.GetRequiredService<ILogger<OpenApiActivityProvider>>();
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var activityLogger = serviceProvider.GetRequiredService<ILogger<OpenApiActivity>>();
    return new OpenApiActivityProvider(parser, logger,activityLogger,httpClientFactory, "OpenApiSpecs");
});


services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));
services.AddRazorPages(options => options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseRouting();
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.MapFallbackToPage("/_Host");
app.Run();