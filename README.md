# TradeBridge 🌉

> A production-grade .NET distributed liquidity bridge simulator engineered for reliability, observability, and scale.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Message%20Broker-FF6600?logo=rabbitmq)](https://www.rabbitmq.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Database-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?logo=docker)](https://www.docker.com/)
[![Kubernetes](https://img.shields.io/badge/Kubernetes-Helm-326CE5?logo=kubernetes)](https://kubernetes.io/)
[![CI Status](https://github.com/kunal/tradebridge/actions/workflows/ci.yml/badge.svg)](https://github.com/kunal/tradebridge/actions/workflows/ci.yml)

---

## Overview
TradeBridge is a suite of decoupled microservices utilizing the asynchronous request-reply pattern. It simulates a distributed trading system that routes orders to external liquidity providers, serving as a comprehensive demonstration of how to handle transient failures, guarantee message delivery, and monitor distributed workflows in a cloud-native environment.

## Why This Project Exists
In high-frequency or distributed trading environments, dropped messages, unhandled network partitions, and lack of visibility can lead to severe financial consequences. TradeBridge exists as a portfolio project to demonstrate the implementation of **Site Reliability Engineering (SRE)** and **Platform Engineering** best practices to solve these distributed systems challenges natively within the .NET ecosystem.

## Key Capabilities
- **Guaranteed Event Delivery:** Prevents data loss during service crashes using the Transactional Outbox pattern.
- **Upstream Resilience:** Protects internal systems from external provider degradation using circuit breakers and exponential backoff.
- **Distributed Traceability:** Injects W3C trace context across HTTP and AMQP boundaries for 100% request visibility.
- **GitOps Ready:** Packaged for Kubernetes deployment with continuous state synchronization via Argo CD.
- **Zero-Touch CI/CD:** Automated testing, containerization, and CVE scanning on every commit.

## Architecture
The system employs an asynchronous, event-driven architecture designed around RabbitMQ and PostgreSQL.

* **API Gateway (`:5003`)**: HTTP ingress for order submissions.
* **Order Service (`:5000`)**: Core orchestrator. Saves state and publishes AMQP events atomically.
* **Risk Service (`:5001`)**: Synchronous mock evaluator for pre-trade risk checks.
* **Execution Service (`:5004`)**: Background worker. Consumes AMQP events and executes REST calls to providers.
* **Liquidity Provider (`:5002`)**: External exchange simulator with configurable fault injection.

*(For detailed architectural diagrams and flow breakdowns, see [docs/architecture.md](docs/architecture.md)).*

## Technology Stack

**Backend:** .NET 8, ASP.NET Core Web API, Entity Framework Core 8
**Infrastructure:** RabbitMQ, PostgreSQL
**Containers:** Docker, Alpine Linux
**CI/CD:** GitHub Actions
**Kubernetes & Deployments:** Helm, Argo CD
**Observability:** OpenTelemetry, Prometheus, Grafana, Zipkin
**Resilience:** Polly, MassTransit
**Security:** Aqua Trivy

## Repository Structure

```text
tradebridge/
├── .github/
│   └── workflows/          # GitHub Actions CI/CD pipelines
├── deploy/
│   ├── argocd/             # GitOps Application definitions
│   └── helm/               # Kubernetes Helm charts
├── docs/                   # Detailed engineering documentation
├── src/                    # .NET Microservices source code
├── tests/                  # xUnit Test suites
├── Dockerfile              # Unified multi-stage container build
├── docker-compose.yml      # Local infrastructure stack
└── run-local.ps1           # One-click local bootstrapper
```

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- PowerShell (Windows) or pwsh (Linux/macOS)

## Quick Start
The repository includes a one-click bootstrap script to launch the full microservice mesh and observability stack locally.

```powershell
# 1. Clone the repository
git clone https://github.com/kunal/tradebridge.git
cd tradebridge

# 2. Build the .NET Solution
dotnet build TradeBridge.sln

# 3. Spin up the infrastructure and services
.\run-local.ps1
```

## Configuration
Configuration is injected via Environment Variables. In local development, these are managed by `appsettings.json`, and in production, they are injected by Helm `values.yaml` (see `deploy/helm/tradebridge/values.yaml`).

Key environment variables:
- `ConnectionStrings__DefaultConnection`: PostgreSQL routing.
- `RabbitMQ__Host`: Message broker DNS.
- `FAILURE_MODE`: Fault injection toggle for the Liquidity Provider (`NONE`, `HTTP_500`, `DELAY`).

## Usage
Once the stack is running, dispatch an order to the API Gateway:

```bash
curl -X POST http://localhost:5003/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{"clientOrderId": "demo-1", "symbol": "EURUSD", "side": 0, "quantity": 1000, "price": 1.1}'
```

Poll the status to see the asynchronous execution transition the order to `FILLED` (Status: 5):
```bash
curl http://localhost:5003/api/v1/orders/demo-1
```

## Local Development
Local development leverages standard `dotnet` CLI tooling. Each service can be run independently:
```bash
dotnet run --project src/TradeBridge.OrderService
```
Dependencies (PostgreSQL, RabbitMQ) can be spun up in isolation using `docker compose up -d postgres rabbitmq`.

## Testing
- **Unit Tests**: Validates domain logic and Risk configurations (`tests/TradeBridge.UnitTests`).
- **Integration Tests**: Validates EF Core data contexts against the database (`tests/TradeBridge.IntegrationTests`).

Execute tests via: `dotnet test`

## CI/CD
The GitHub Actions workflow (`.github/workflows/ci.yml`) enforces the following pipeline:
`Commit → Build Solution → Run xUnit Tests → Concurrent Docker Matrix Build → Aqua Trivy Security Scan`

## Kubernetes / Deployment
The platform is packaged natively for Kubernetes via Helm. 

To deploy manually:
```bash
helm upgrade --install tradebridge ./deploy/helm/tradebridge --namespace tradebridge --create-namespace
```

Alternatively, apply the Argo CD application for GitOps reconciliation:
```bash
kubectl apply -f deploy/argocd/application.yaml
```

## Security
- **Container Scanning**: Aqua Trivy scans for CVEs in the base Alpine images and .NET dependencies during CI.
- **Minimal Attack Surface**: Docker images are built as rootless, distroless-inspired Alpine containers.

## Observability
The stack is fully instrumented with OpenTelemetry.
- **Metrics**: ASP.NET Core and HTTP client metrics are scraped by **Prometheus** (`localhost:9090`) and visualized in **Grafana** (`localhost:3000`).
- **Distributed Traces**: W3C trace contexts span HTTP and AMQP calls, viewable in **Zipkin** (`localhost:9411`).

## Reliability & SLOs
The system is built to sustain a 99.9% availability target on the API Gateway, even if downstream dependencies (Liquidity Providers) experience total failure. We track the SLI of "Successful API Ingress" regardless of asynchronous execution latency.

## Failure & Recovery
- **Database Partition**: MassTransit Outbox buffers outgoing messages in EF Core.
- **Broker Outage**: Application continues accepting HTTP requests, queueing messages locally in the Outbox until RabbitMQ recovers.
- **Provider Outage**: Execution Service Polly policies trip the circuit breaker, failing transactions gracefully without exhausting threads.

## Design Trade-offs
- **Why RabbitMQ over Kafka?** For transactional outbox patterns in .NET, RabbitMQ's AMQP model with MassTransit offers superior developer ergonomics and exact queue semantics compared to Kafka's log-based approach, which was overkill for this specific throughput target.
- **Why Zipkin over Jaeger?** Zipkin was chosen for local development due to its minimal footprint and instant startup in Docker Compose, though OTel allows swapping to Jaeger/Tempo in production with zero code changes.

## Known Limitations
- The Outbox pattern currently runs in the same database as the Order state. Under extreme load, this table could become a bottleneck and require sharding.
- Custom domain metrics (e.g., `orders_failed_total`) are not yet implemented; observability relies on standard framework HTTP/EF metrics.

## Roadmap
- [x] RabbitMQ asynchronous messaging
- [x] Transactional Outbox implementation
- [x] OpenTelemetry metrics and traces
- [x] Helm & Argo CD packaging
- [ ] Implement custom business-logic Prometheus meters
- [ ] Add Redis distributed caching for Risk Service

## Contributing
This is a personal portfolio project. While I am not accepting functional PRs, feedback and architectural reviews via GitHub Issues are highly encouraged.

## License
MIT License. See `LICENSE` for details.

## Author
**Kunal**
- GitHub: [@kunal](https://github.com/kunal)
