# EuropeChat Observability Stack

This project implements a complete observability stack for the EuropeChat application using OpenTelemetry, Prometheus, and Grafana.

## Architecture

```
External Traffic → EuropeChat.Proxy:5240 → EuropeChat.Api:5185 → Database
                   ↓
              OpenTelemetry Observability
              (Traces, Metrics, Logs)
                   ↓
              OTLP Collector → Jaeger (Traces)
                   ↓            ↓
              Prometheus ← Metrics → Grafana
```

## Components

### EuropeChat.Proxy
- **Port**: 5240 (configurable via `APP_PORT`)
- **Purpose**: HTTP proxy that forwards all traffic to the API
- **Observability**: Implements OpenTelemetry exactly as per Microsoft documentation
  - **Tracing**: ASP.NET Core + HTTP Client + Custom spans
  - **Metrics**: Request counters, histograms, runtime metrics
  - **Logging**: Structured logging with trace correlation

### EuropeChat.Api
- **Port**: 5185 (internal)
- **Purpose**: Business logic without any observability overhead
- **Note**: All OpenTelemetry instrumentation has been removed

### Observability Stack
- **Jaeger**: Distributed tracing UI (port 16686)
- **Prometheus**: Metrics collection (port 9090)
- **Grafana**: Visualization and dashboards (port 3000)
- **OTLP Collector**: OpenTelemetry data collection (ports 4317/4318)

## Usage

### Starting the Stack
```bash
docker compose up -d
```

### Accessing Services
- **Application**: http://localhost:5240 (through proxy)
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **Jaeger**: http://localhost:16686

### Endpoints
- **Health Check**: `GET /health` (proxy only)
- **Metrics**: `GET /metrics` (proxy only)
- **All API endpoints**: Proxied transparently

### Example API Calls
```bash
# Health check
curl http://localhost:5240/health

# API endpoints (proxied)
curl http://localhost:5240/api/chat
curl http://localhost:5240/api/conversations

# View metrics
curl http://localhost:5240/metrics
```

## OpenTelemetry Implementation

The proxy implements OpenTelemetry following the official Microsoft documentation example:

### Metrics
- **greetings.count**: Request counter
- **request_duration**: Request duration histogram
- **Built-in ASP.NET Core metrics**: HTTP requests, Kestrel, etc.
- **Built-in .NET metrics**: GC, ThreadPool, etc.

### Traces
- **Incoming requests**: ASP.NET Core instrumentation
- **Outgoing requests**: HTTP Client instrumentation
- **Custom spans**: Proxy operation tracing

### Logs
- **Structured logging**: JSON format with trace correlation
- **Request/response logging**: Detailed proxy operation logs

## Monitoring

### Prometheus Queries
```promql
# Request rate
rate(greetings_count_total[5m])

# 95th percentile latency
histogram_quantile(0.95, rate(request_duration_bucket[5m]))

# Error rate
rate(greetings_count_total{status_code!~"2.."}[5m])
```

### Grafana Dashboards
- Pre-configured Prometheus datasource
- Basic proxy metrics dashboard
- Customizable and extendable

## Configuration

### Environment Variables
- `APP_PORT`: Proxy port (default: from compose.yaml)
- `OTLP_ENDPOINT_URL`: OpenTelemetry collector endpoint
- `ApiBaseUrl`: Target API URL for proxying

### Development vs Production
- **Development**: Direct localhost connections
- **Docker**: Service-to-service communication via container names

## Extending the Setup

### Adding Custom Metrics
```csharp
var customMeter = new Meter("MyApp.Custom", "1.0.0");
var myCounter = customMeter.CreateCounter<int>("my_metric");
myCounter.Add(1);
```

### Adding Custom Traces
```csharp
using var activity = greeterActivitySource.StartActivity("MyOperation");
activity?.SetTag("custom.data", value);
```

### Adding Grafana Dashboards
1. Create JSON dashboard files in `grafana/provisioning/dashboards/`
2. Restart Grafana service
3. Dashboards will be automatically loaded

## Troubleshooting

### Common Issues
1. **Metrics not appearing**: Check Prometheus targets at http://localhost:9090/targets
2. **Traces not visible**: Verify OTLP collector is running and endpoint is correct
3. **Grafana not loading dashboards**: Check provisioning volume mounts

### Logs
```bash
# View proxy logs
docker compose logs europechat-proxy

# View all services
docker compose logs
```

## Performance Considerations

- **Sampling**: All traces are currently sampled (adjust for production)
- **Metrics retention**: Prometheus default retention (adjust as needed)
- **Grafana**: Uses persistent storage for dashboards and settings 