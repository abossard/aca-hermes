# Hermes Load Testing with Locust

This directory contains Locust load tests for the Hermes ASP.NET Aspire application.

## Prerequisites

1. Python 3.8+ installed
2. Hermes Aspire application running (`dotnet run --project src/Hermes.AppHost`)

## Setup

```bash
# Create virtual environment
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt
```

## Running Load Tests

### Option 1: Web UI Mode (Recommended)

```bash
cd load-tests
locust
```

Then open http://localhost:8089 in your browser to configure and start the load test.

### Option 2: Headless Mode

```bash
# Test all services (default HermesUser class)
locust --headless --users 10 --spawn-rate 2 --run-time 60s --host http://localhost:5000

# Test specific service endpoints
locust --headless --users 5 --spawn-rate 1 --run-time 30s --host http://localhost:5001 -f locustfile.py ProxyServiceUser
locust --headless --users 5 --spawn-rate 1 --run-time 30s --host http://localhost:5002 -f locustfile.py ApiServiceUser
```

### Option 3: Using Configuration File

```bash
locust --config locust.conf
```

## User Classes

The `locustfile.py` contains three user classes:

1. **HermesUser** (Default): Tests the complete application flow including proxy and API calls
2. **ApiServiceUser**: Focused load testing of the API service only  
3. **ProxyServiceUser**: Focused load testing of the proxy service only

## Test Scenarios

### HermesUser Tasks:
- **Direct API calls** (weight: 3): Tests `/weatherforecast` endpoint directly
- **Proxy external calls** (weight: 2): Tests proxy with external httpbin.org APIs
- **Proxy internal calls** (weight: 4): Tests proxy with internal service discovery
- **Health checks** (weight: 1): Tests `/health` endpoints

### Customizing Tests

You can modify the following in `locustfile.py`:

- **Task weights**: Change the numbers in `@task(n)` decorators
- **Wait times**: Modify `wait_time = between(x, y)` 
- **Target URLs**: Add/remove URLs in the test methods
- **Custom headers**: Add headers to requests if needed

## Monitoring

While tests run, monitor:

1. **Locust Web UI**: http://localhost:8089 for real-time metrics
2. **Aspire Dashboard**: Check the Aspire dashboard for service health and telemetry
3. **System resources**: Monitor CPU, memory, and network usage

## Integration with Azure Load Testing

To use these tests with Azure Load Testing:

1. Zip the entire `load-tests` directory
2. Upload to Azure Load Testing service
3. Configure engine instances and load parameters in Azure portal

## Tips

- Start with low user counts (5-10) and gradually increase
- Monitor your system resources during tests
- The chaos engineering in ApiService will introduce random delays/failures
- Use different user classes to isolate performance bottlenecks