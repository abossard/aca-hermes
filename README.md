# Hermes - .NET Aspire Proxy Service

## Core Principles
- A Philosophy of Software Design by John Ousterhout
- Grokking Simplicity by Eric Normand

## Architecture

This project demonstrates a microservices architecture using .NET Aspire with:

- **Hermes.ApiService**: Weather API with chaos engineering middleware (random delays/failures)
- **Hermes.Proxy**: Lightweight HTTP proxy service with **Native AOT support** for minimal footprint and fast startup
- **Hermes.Web**: Blazor Server frontend with interactive testing capabilities
- **Hermes.AppHost**: .NET Aspire orchestration with service discovery and health checks
- **Hermes.ServiceDefaults**: Shared OTEL configuration for observability
- **Hermes.Tests**: End-to-end black box integration tests

## Running the Application

```bash
dotnet run --project src/Hermes.AppHost
```

This will start all services with the Aspire dashboard available for monitoring and service discovery.

## Native AOT Deployment

The **Hermes.Proxy** service supports Native AOT compilation for production deployments with minimal footprint and fast startup:

### Build Native AOT Binary

```bash
# For Linux (e.g., containers, cloud deployments)
dotnet publish src/Hermes.Proxy -c Release -r linux-x64

# For Windows
dotnet publish src/Hermes.Proxy -c Release -r win-x64

# For macOS
dotnet publish src/Hermes.Proxy -c Release -r osx-x64
```

### Benefits of Native AOT Proxy

- **🚀 Fast Startup**: ~10ms cold start vs ~1000ms+ for regular .NET
- **📦 Small Size**: ~76KB executable + dependencies vs full .NET runtime
- **💾 Low Memory**: Reduced memory footprint for containerized environments
- **🔒 Self-Contained**: No .NET runtime installation required on target machine
- **☁️ Cloud-Ready**: Perfect for serverless and container deployments

The published binary can run standalone without any .NET installation on the target system.

## Testing the Proxy

1. Navigate to the web frontend (URL shown in Aspire dashboard)
2. Use the "Call API Directly" button to test direct API communication
3. Use the "Call API via Proxy" button to test requests routed through the proxy service

## Proxy API

The proxy service exposes a simple HTTP GET endpoint:

```
GET /proxy?url={target_url}
```

- Validates the provided URL format
- Forwards the request to the target endpoint with **60-second timeout**
- Returns the **raw response** with original status codes and content types (1:1 pass-through)
- **No retries or resilience** - pure proxy behavior
- Logs all requests using OpenTelemetry for observability

### Example Usage

#### External API Call
```bash
curl -s "http://localhost:5001/proxy?url=https%3A//httpbin.org/json" | jq '.'
```

**Response:**
```json
{
  "slideshow": {
    "author": "Yours Truly",
    "date": "date of publication",
    "slides": [
      {
        "title": "Wake up to WonderWidgets!",
        "type": "all"
      }
    ],
    "title": "Sample Slide Show"
  }
}
```

#### Internal Service Call via Service Discovery
```bash
curl -s "http://localhost:5001/proxy?url=https%2Bhttp%3A//apiservice/weatherforecast" | jq '.'
```

**Response:**
```json
[
  {
    "date": "2024-01-15",
    "temperatureC": 22,
    "temperatureF": 71,
    "summary": "Mild"
  },
  {
    "date": "2024-01-16", 
    "temperatureC": 18,
    "temperatureF": 64,
    "summary": "Cool"
  },
  {
    "date": "2024-01-17",
    "temperatureC": 31,
    "temperatureF": 87,
    "summary": "Hot"
  },
  {
    "date": "2024-01-18",
    "temperatureC": 14,
    "temperatureF": 57,
    "summary": "Chilly"
  },
  {
    "date": "2024-01-19",
    "temperatureC": 26,
    "temperatureF": 78,
    "summary": "Warm"
  }
]
```

#### Testing with Different Endpoints
```bash
# Test with HTTP status codes
curl -s "http://localhost:5001/proxy?url=https%3A//httpbin.org/status/200" 

# Test with delay simulation
curl -s "http://localhost:5001/proxy?url=https%3A//httpbin.org/delay/2"

# Test with different content types
curl -s "http://localhost:5001/proxy?url=https%3A//httpbin.org/xml" | xmllint --format -
```

**Service Discovery**: The proxy service has a `WithReference(apiService)` in the AppHost configuration, which enables it to resolve `https+http://apiservice` URLs to the actual service endpoints automatically.

#### Error Handling Examples

**Invalid URL:**
```bash
curl -s "http://localhost:5001/proxy?url=invalid-url"
```
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request", 
  "status": 400,
  "detail": "Invalid URL format"
}
```

**HTTP Error (Raw Pass-through):**
```bash
curl -s "http://localhost:5001/proxy?url=https%3A//httpbin.org/status/500"
```
```
Internal Server Error
```
*Returns HTTP 500 status with original response body*

**Network/Timeout Error:**
```bash
curl -s "http://localhost:5001/proxy?url=http%3A//localhost%3A99999/nonexistent"
```
```
Proxy Error: Connection refused
```
*Returns HTTP 502 Bad Gateway*

## Azure Container Apps Scaling

.NET Aspire 9.4+ provides seamless integration with Azure Container Apps scaling rules, supporting:
- HTTP-based scaling with concurrent request limits
- Minimum and maximum replica configuration
- Automatic KEDA integration for event-driven scaling
- DataProtection configuration for multi-instance deployments

# Todo

(For each task: Search and make a plan in a separate markdown file with mermaid diagram)
- [x] Update the ApiService so that e.g. 10% of the requests will take longer to respond and some will randomly fail. ([plan](docs/plan-chaos-apiserver.md))
- [x] Create a super efficient, lean dotnet based Proxy called Hermes.Proxy
    - [x] This proxy gets a url= query parameter and will call this endpoint and return the result
    - [x] It should used the normal dotnet OTEL based logging to log requests
- [x] Update Hermes.Web to have some button to call the ApiService directly and via proxy
- [x] Can Aspire.NET Azure Container Apps integration do scaling rules?
- [x] write an end to end test that calls the proxy and verifies the result 