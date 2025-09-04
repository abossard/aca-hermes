var builder = DistributedApplication.CreateBuilder(args);

// Check if running in Azure deployment (azd up) vs local development
var isAzureDeployment = builder.Environment.EnvironmentName != "Development";

// Build API Service
var apiServiceBuilder = builder.AddProject<Projects.Hermes_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

// Build Proxy Service  
var proxyServiceBuilder = builder.AddProject<Projects.Hermes_Proxy>("proxy")
    .WithHttpHealthCheck("/health")
    .WithReference(apiServiceBuilder);  // Proxy needs reference to apiservice for service discovery

// Build Web Service
var webServiceBuilder = builder.AddProject<Projects.Hermes_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiServiceBuilder)
    .WithReference(proxyServiceBuilder)
    .WaitFor(apiServiceBuilder)
    .WaitFor(proxyServiceBuilder);

// Only add Azure-specific configuration when deploying to Azure
if (isAzureDeployment)
{
    // Provision Application Insights for Azure deployment
    var insights = builder.AddAzureApplicationInsights("insights");
    
    // Add Application Insights references to all services
    apiServiceBuilder = apiServiceBuilder.WithReference(insights);
    proxyServiceBuilder = proxyServiceBuilder.WithReference(insights);
    webServiceBuilder = webServiceBuilder.WithReference(insights);
    
    // Set replicas for Azure deployment (local uses 1)
    proxyServiceBuilder = proxyServiceBuilder.WithReplicas(40);
}

builder.Build().Run();
