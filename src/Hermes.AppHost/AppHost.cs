var builder = DistributedApplication.CreateBuilder(args);

// More reliable way to detect Azure deployment using environment variable or configuration
var isAzureDeployment = !string.IsNullOrEmpty(builder.Configuration["AZURE_DEPLOYMENT"]) || 
                       builder.Configuration["ASPNETCORE_ENVIRONMENT"] == "Production";

// Build services first
var apiServiceBuilder = builder.AddProject<Projects.Hermes_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

var proxyServiceBuilder = builder.AddProject<Projects.Hermes_Proxy>("proxy")
    .WithHttpHealthCheck("/health")
    .WithReference(apiServiceBuilder);  // Proxy needs reference to apiservice for service discovery

var webServiceBuilder = builder.AddProject<Projects.Hermes_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiServiceBuilder)
    .WithReference(proxyServiceBuilder)
    .WaitFor(apiServiceBuilder)
    .WaitFor(proxyServiceBuilder);

// Add Locust load testing container for both local and Azure deployment
var locustContainer = builder.AddDockerfile("locust", Path.Combine(builder.AppHostDirectory, "../../load-tests"))
    .WithHttpEndpoint(port: 8089, targetPort: 8089, name: "web-ui")
    .WithExternalHttpEndpoints()
    .WithEnvironment("LOCUST_HOST", proxyServiceBuilder.GetEndpoint("http"))
    .WithEnvironment("LOCUST_HEADLESS", "false")
    .WithEnvironment("LOCUST_USERS", "10")
    .WithEnvironment("LOCUST_SPAWN_RATE", "2")
    .WaitFor(proxyServiceBuilder);

// Only add Azure-specific configuration when deploying to Azure
if (isAzureDeployment)
{
    // Only provision Application Insights for Azure deployment
    var insights = builder.AddAzureApplicationInsights("insights");
    
    // Add Application Insights references to all services
    apiServiceBuilder = apiServiceBuilder.WithReference(insights).WithReplicas(100);
    proxyServiceBuilder = proxyServiceBuilder.WithReference(insights);
    webServiceBuilder = webServiceBuilder.WithReference(insights);
    
    // Set replicas for Azure deployment (local uses 1)
    proxyServiceBuilder = proxyServiceBuilder.WithReplicas(100);
}

builder.Build().Run();
