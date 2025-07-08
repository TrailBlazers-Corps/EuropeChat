# EuropeChat Proxy - Grafana Dashboard Guide

## Overview

The **EuropeChat Proxy - Comprehensive Dashboard** provides detailed monitoring and observability for the proxy service that handles all incoming requests and forwards them to the API backend. This dashboard gives you real-time insights into performance, traffic patterns, and infrastructure health.

## Dashboard Sections

### 📊 **Top Row - Key Performance Indicators**

#### 1. **Request Rate** (requests/sec)
- **Purpose**: Shows the current rate of incoming requests per second
- **Thresholds**: 
  - 🟢 Green: < 10 req/s (Normal)
  - 🟡 Yellow: 10-50 req/s (Moderate)
  - 🔴 Red: > 50 req/s (High load)
- **What to watch**: Sudden spikes might indicate traffic surges or potential DDoS

#### 2. **Total Requests**
- **Purpose**: Cumulative count of all requests processed since startup
- **Use case**: Track overall usage and growth patterns

#### 3. **Average Response Time** (ms)
- **Purpose**: Shows the average time taken to process requests
- **Thresholds**:
  - 🟢 Green: < 100ms (Excellent)
  - 🟡 Yellow: 100-500ms (Acceptable)
  - 🔴 Red: > 500ms (Poor performance)
- **What to watch**: Increasing response times may indicate backend issues

#### 4. **Active Requests**
- **Purpose**: Number of requests currently being processed
- **Thresholds**:
  - 🟢 Green: < 10 (Normal)
  - 🟡 Yellow: 10-50 (Busy)
  - 🔴 Red: > 50 (Overloaded)
- **What to watch**: High values might indicate bottlenecks

### 📈 **Performance Analysis Section**

#### 5. **Request Rate Over Time**
- **Purpose**: Time-series view of request rate broken down by HTTP method
- **Legend**: Different colored lines for GET, POST, etc.
- **Use case**: Identify traffic patterns and peak usage times

#### 6. **Response Time Percentiles**
- **Purpose**: Shows 50th, 95th, and 99th percentile response times
- **Why important**: 
  - **50th percentile**: Median user experience
  - **95th percentile**: Performance for most users
  - **99th percentile**: Worst-case scenarios
- **What to watch**: Increasing percentiles indicate performance degradation

### 🔍 **Traffic Analysis Section**

#### 7. **Requests by Path**
- **Purpose**: Shows which API endpoints are being called most frequently
- **Legend**: Different lines for `/api/chat`, `/api/conversations`, etc.
- **Use case**: Identify popular endpoints and potential bottlenecks

#### 8. **Status Code Distribution**
- **Purpose**: Tracks HTTP status codes returned by the proxy
- **Legend**: 
  - **200**: Successful requests
  - **4xx**: Client errors (bad requests)
  - **5xx**: Server errors (proxy/backend issues)
- **What to watch**: Increasing 4xx or 5xx rates indicate problems

### 🏗️ **Infrastructure Monitoring Section**

#### 9. **Kestrel Active Connections**
- **Purpose**: Number of active HTTP connections to the proxy
- **Thresholds**:
  - 🟢 Green: < 100 (Normal)
  - 🟡 Yellow: 100-500 (Busy)
  - 🔴 Red: > 500 (Very busy)

#### 10. **Queued Connections**
- **Purpose**: Connections waiting to be processed
- **Thresholds**:
  - 🟢 Green: 0-10 (Normal)
  - 🟡 Yellow: 10-50 (Busy)
  - 🔴 Red: > 50 (Bottleneck)
- **What to watch**: High queue means the server can't keep up

#### 11. **Client Active Requests** (To API)
- **Purpose**: Outbound requests from proxy to backend API
- **Thresholds**:
  - 🟢 Green: < 10 (Normal)
  - 🟡 Yellow: 10-25 (Busy)
  - 🔴 Red: > 25 (Backend bottleneck)

#### 12. **Open Client Connections**
- **Purpose**: Open connections from proxy to backend API
- **Thresholds**:
  - 🟢 Green: < 50 (Normal)
  - 🟡 Yellow: 50-100 (High usage)
  - 🔴 Red: > 100 (Connection pooling issues)

### ⏱️ **Detailed Performance Section**

#### 13. **Connection Duration**
- **Purpose**: How long HTTP connections stay open
- **Metrics**:
  - **95th percentile**: Longest connections
  - **Average**: Typical connection duration
- **Use case**: Identify connection management issues

#### 14. **Client Request Duration (to API)**
- **Purpose**: Time taken for proxy to communicate with backend API
- **Metrics**:
  - **95th percentile**: Worst-case backend response time
  - **Average**: Typical backend performance
- **What to watch**: High values indicate backend API performance issues

## 🚨 **Alerting Scenarios**

### Critical Issues
- Response time > 1000ms consistently
- Error rate > 5%
- Queue depth > 100
- No successful requests for > 1 minute

### Warning Signs
- Response time trending upward
- Increasing 95th percentile response times
- Growing number of active connections
- Backend request duration increasing

## 📱 **Dashboard Usage Tips**

### Time Range Controls
- Use **5s** refresh for real-time monitoring
- Switch to **15m-1h** time range for trend analysis
- Use **1d-1w** for capacity planning

### Correlation Analysis
- Compare **Request Rate** with **Response Time** to identify capacity limits
- Monitor **Client Request Duration** alongside **Response Time** to isolate frontend vs backend issues
- Watch **Active Requests** vs **Queued Connections** for bottleneck identification

### Troubleshooting Workflow
1. **High Response Time**: Check backend request duration first
2. **High Error Rate**: Look at status code distribution
3. **Performance Issues**: Compare current metrics with historical baselines
4. **Capacity Planning**: Monitor peak request rates and connection counts

## 🔗 **Related Services**

- **Prometheus**: `http://localhost:9090` - Raw metrics and queries
- **Jaeger**: `http://localhost:16686` - Distributed tracing
- **API Health**: `http://localhost:5185` - Direct API access (for comparison)
- **Proxy Health**: `http://localhost:8080/health` - Proxy health check

## 🎯 **Dashboard Access**

- **URL**: `http://localhost:3000`
- **Login**: admin / admin
- **Dashboard**: "EuropeChat Proxy - Comprehensive Dashboard"
- **Auto-refresh**: 5 seconds (configurable) 