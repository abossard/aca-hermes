# Plan: Introduce Chaos in Hermes.ApiService

Goal: Make ~10% of requests slower and a small percentage randomly fail, to simulate real-world instability and exercise resilience.

## Scope and Approach
- Add a configurable middleware in `Hermes.ApiService` that:
  - Optionally short-circuits a request with a configured failure status code based on probability.
  - Optionally adds an artificial delay based on probability and range.
  - Excludes health and OpenAPI endpoints from chaos.
  - Emits structured logs and works with existing OpenTelemetry setup.
- Surface configuration via `appsettings` (with sensible defaults) and bind via options.

## Assumptions
- "Some will randomly fail" interpreted as 5% by default. Adjustable via config.
- Chaos applies to all HTTP methods except excluded paths (`/health`, `/alive`, `/openapi`).
- No exceptions thrown for failures; respond with status code to keep exception pipeline quiet.

## Config Shape
```json
{
  "Chaos": {
    "Enabled": true,
    "DelayPercentage": 0.10,
    "DelayMillisecondsMin": 200,
    "DelayMillisecondsMax": 1500,
    "FailurePercentage": 0.05,
    "FailureStatusCode": 500,
    "ExcludedPathPrefixes": ["/health", "/alive", "/openapi"]
  }
}
```

## Middleware Logic
- If `Enabled` is false or path matches any `ExcludedPathPrefixes`, pass through.
- Draw a single random double for failure; if under `FailurePercentage`, log and short-circuit with `FailureStatusCode`.
- Else draw a random double for delay; if under `DelayPercentage`, compute random delay between min/max and `await Task.Delay`.
- Proceed to next middleware/endpoint.

## Mermaid Diagram
```mermaid
flowchart TD
    A[Incoming Request] --> B{Chaos Enabled?}
    B -- No --> Z[Next Middleware/Endpoint]
    B -- Yes --> C{Path Excluded? (/health,/alive,/openapi)}
    C -- Yes --> Z
    C -- No --> D{Random < Failure%?}
    D -- Yes --> E[Log and Return Failure Status]
    D -- No --> F{Random < Delay%?}
    F -- Yes --> G[Delay rand(msMin..msMax)] --> Z
    F -- No --> Z
```

## Test Cases
- Health endpoint is never delayed or failed.
- With default config, ~5% 500 responses over many requests, ~10% delayed.
- Setting `Enabled=false` disables chaos entirely.

## Rollout
- Add middleware and config, build and run locally.
- Metrics/Tracing: rely on existing OTEL; failures create standard ASP.NET request spans with non-200 status.