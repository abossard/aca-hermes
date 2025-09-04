// Use CreateSlimBuilder for AOT compatibility - only essential features
var builder = WebApplication.CreateSlimBuilder(args);

// Configure HTTPS for slim builder when using https:// addresses
builder.WebHost.UseKestrelHttpsConfiguration();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Configure HttpClient with service discovery support - no resilience, raw pass-through
builder.Services.AddHttpClient("ProxyClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60); // Simple timeout, no retries
});

// Essential services for slim builder
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = true;
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Note: OpenAPI might not be AOT compatible, consider removing for production
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline for AOT
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Proxy endpoint that accepts url parameter and forwards requests
app.MapGet("/proxy", async (string url, IHttpClientFactory httpClientFactory, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Proxying request to {Url}", url);

        // Validate URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri))
        {
            logger.LogWarning("Invalid URL provided: {Url}", url);
            return Results.BadRequest("Invalid URL format");
        }

        // Create HttpClient with service discovery support
        using var httpClient = httpClientFactory.CreateClient("ProxyClient");
        
        // Forward the request and return raw response
        var response = await httpClient.GetAsync(targetUri);
        var content = await response.Content.ReadAsStringAsync();
        
        logger.LogInformation("Proxy request completed. Status: {StatusCode}, URL: {Url}, ContentLength: {ContentLength}", 
            response.StatusCode, url, content?.Length ?? 0);

        // Return raw response with original status code and content type
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "text/plain";
        return Results.Text(content, contentType, statusCode: (int)response.StatusCode);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error proxying request to {Url}", url);
        // Return 502 Bad Gateway for any proxy errors
        return Results.Text($"Proxy Error: {ex.Message}", "text/plain", statusCode: 502);
    }
})
.WithName("ProxyRequest")
.WithSummary("Proxy HTTP requests to the specified URL")
.WithDescription("Accepts a 'url' query parameter and forwards the HTTP request to that endpoint");

app.MapDefaultEndpoints();

app.Run();
