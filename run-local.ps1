$ErrorActionPreference = 'Stop'

Write-Host "Starting Infrastructure (PostgreSQL, RabbitMQ, Zipkin, Prometheus, Grafana)..." -ForegroundColor Cyan
docker compose up -d

Write-Host "Waiting for infrastructure to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host "Starting TradeBridge Microservices..." -ForegroundColor Cyan

$services = @(
    @{ Name = "ApiGateway"; Port = 5003 },
    @{ Name = "OrderService"; Port = 5000 },
    @{ Name = "RiskService"; Port = 5001 },
    @{ Name = "ExecutionService"; Port = 5004 },
    @{ Name = "LiquidityProvider"; Port = 5002 }
)

$processes = @()

foreach ($svc in $services) {
    Write-Host "Starting $($svc.Name) on port $($svc.Port)..."
    $proc = Start-Process dotnet -ArgumentList "run --no-build --project src/TradeBridge.$($svc.Name) --urls=http://localhost:$($svc.Port)" -PassThru -WindowStyle Minimized
    $processes += $proc
}

Write-Host "All services started!" -ForegroundColor Green
Write-Host "API Gateway: http://localhost:5003"
Write-Host "Prometheus: http://localhost:9090"
Write-Host "Grafana: http://localhost:3000"
Write-Host "Zipkin: http://localhost:9411"
Write-Host ""
Write-Host "Press ENTER to stop all services..." -ForegroundColor Yellow

Read-Host

Write-Host "Stopping services..." -ForegroundColor Cyan
foreach ($proc in $processes) {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}

Write-Host "Stopping infrastructure..." -ForegroundColor Cyan
docker compose stop

Write-Host "Done." -ForegroundColor Green
