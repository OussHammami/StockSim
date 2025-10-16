# StockSim — Onboarding Brief

## Context

Goal: a portfolio trading simulator to learn **Blazor Server**, **React**, **RabbitMQ**, **Docker**, **Kubernetes (k3d)**, and basic cloud practices. Target runtime: **.NET 8**.

## Current State (≈70% complete)

* **Projects**

  * `StockSim.Domain` — entities (Order, Position…), value models (Quote, Snapshot).
  * `StockSim.Application` — abstractions (e.g., `IOrderPublisher`, `IPortfolioService`), DTOs.
  * `StockSim.Infrastructure`

    * Persistence: **EF Core 8** + **Npgsql** (PostgreSQL). Migrations live here.
    * Messaging: RabbitMQ connection, `OrderPublisher`.
    * Outbox: table + dispatcher background service.
  * `StockSim.MarketFeed` — ASP.NET API + SignalR quotes hub; simulated prices.
  * `StockSim.Web` — **Blazor Server** app with Identity, MudBlazor UI, orders hub, dashboard.
  * `StockSim.React` — Vite app for charts/leaderboard (skeleton).
* **Data**

  * DB: **PostgreSQL** (Bitnami chart in k8s; container in compose).
  * EF pattern: **AddDbContextPool** for Identity, **AddPooledDbContextFactory** for app services. Create a **fresh context per method**.
* **Messaging**

  * **RabbitMQ** dev Deployment (management image). `OrderPublisher` emits commands. `OrderConsumer` consumes and writes domain effects.
  * **Outbox** pattern: DB table stores events; `OutboxDispatcher` flushes to **SignalR** hub with idempotency.
* **UI**

  * MudBlazor theme, dashboard:

    * Quotes table (live via SignalR).
    * Portfolio snapshot (cash, positions, PnL).
    * Trade box (Buy/Sell).
    * Orders table with **pagination**.
  * **SignalR**:

    * Orders hub on **Web** (same-origin for browser).
    * Quotes hub on **MarketFeed**.
* **Ops**

  * Health: `/healthz` (liveness), `/readyz` (DB + Rabbit tags).
  * Security: antiforgery, Identity cookies, basic CSP, forwarded headers, conditional HTTPS redirect.
  * Telemetry: **OpenTelemetry** tracing to **Zipkin**; metrics to **Prometheus**; **Grafana** datasource configured.
* **Containers**

  * `docker-compose.yml` for local dev.
* **Kubernetes (k3d)**

  * k3d cluster with Traefik on :80.
  * Namespace `stocksim`.
  * Manifests: `web`, `marketfeed`, `rabbitmq`, Ingress. Postgres via Bitnami Helm.
  * Readiness/liveness probes wired. Orders hub uses same-origin, quotes hub uses internal DNS.

## What’s Done

* Blazor Server app with Identity + MudBlazor.
* Dashboard with live quotes, snapshot, buy/sell, orders pagination.
* MarketFeed simulator + SignalR quotes hub.
* RabbitMQ publisher/consumer; **OutboxDispatcher** to hub.
* PostgreSQL integration; EF migrations applied.
* Health endpoints + probes.
* Zipkin, Prometheus, Grafana wired.
* Docker Compose dev runs end-to-end.
* k3d manifests applied; hosts `stocksim.local` / `marketfeed.local` work.

## What’s Left

1. **Order matcher v1**

   * Support Limit and Stop orders, partial fills, `Remaining`, status transitions (Pending → Partial → Filled/Rejected/Canceled).
   * Order book view and simple matching loop.
2. **React app**

   * Live chart (candles + volume) from market feed.
   * Leaderboard (server API, paging, sort).
   * Polish error/empty states.
3. **Tests**

   * Unit tests for domain.
   * Integration tests with **Testcontainers** (Postgres, Rabbit).
   * Basic e2e (Playwright): login, dashboard, buy/sell.
4. **CI/CD**

   * GitHub Actions: build, test, docker build. Optional push to registry.
5. **K8s prod polish**

   * TLS termination, HSTS on.
   * CPU/mem requests+limits; optional HPA.
   * Postgres PVCs.
   * Secrets management.
   * Prometheus alerts; Grafana dashboard.
6. **Cloud**

   * AKS/EKS deploy path (Helm or kustomize overlays).

## Key Design Choices

* **EF usage:** never inject `ApplicationDbContext` into app services. Use `IDbContextFactory<ApplicationDbContext>`. No parallel queries on one context. Materialize inside method.
* **Outbox:** persist integration events, dispatch in background, mark processed; hub messages are **idempotent**.
* **SignalR URLs:**

  * Orders hub (browser) → **relative path** `"/hubs/orders?..."` (Ingress same-origin).
  * Quotes hub (browser) → public host **`http://marketfeed.local/hubs/quotes`** in k8s; **`http://marketfeed:8080`** only for **server-to-server**.
* **K8s DNS vs browser:** cluster DNS like `http://web:8080` is **only for pods**. Browsers must use Ingress hosts.
* **Forwarded headers:** required behind Traefik; HTTPS redirect only in non-Dev.

## Repo Layout (expected)

```
src/
  StockSim.Domain/
  StockSim.Application/
    Abstractions/
    Contracts/
  StockSim.Infrastructure/
    Persistence/
      ApplicationDbContext.cs
      Migrations/
      Identity/ (ApplicationUser)
    Messaging/
      RabbitConnection.cs
      OrderPublisher.cs
    Services/
      PortfolioService.cs
      OrderService.cs
    Outbox/
      OutboxMessage.cs
      OutboxDispatcher.cs
  StockSim.MarketFeed/
    Program.cs (quotes hub + health)
  StockSim.Web/
    Components/Pages/Dashboard.razor
    Hubs/OrderHub.cs
    Services/OrderConsumer.cs
    Program.cs + StartupExtensions.cs
  StockSim.React/
k8s/
  k3d.yaml
  web.yaml
  marketfeed.yaml
  rabbitmq.yaml
  ingress.yaml
ops/
  prometheus.yml
  grafana/ (if any)
docker-compose.yml
```

## Critical Config

### EF registrations (Infrastructure)

```csharp
// same options in both
services.AddDbContextPool<ApplicationDbContext>(o => o.UseNpgsql(cs));
services.AddPooledDbContextFactory<ApplicationDbContext>(o => o.UseNpgsql(cs));
```

### Web pipeline bits (Program.cs)

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions {
  ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
});
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapHub<OrderHub>("/hubs/orders");
```

### Dashboard hub setup (browser)

```csharp
// Orders hub: same-origin
var hubUrl = NavigationManager.ToAbsoluteUri($"/hubs/orders?userId={Uri.EscapeDataString(uid)}");
_ordersHub = new HubConnectionBuilder().WithUrl(hubUrl).WithAutomaticReconnect().Build();

// Quotes hub: MarketFeed public host in k8s
_quoteshub = new HubConnectionBuilder().WithUrl($"{Config["MarketFeed:BaseUrl"]}/hubs/quotes")
                                       .WithAutomaticReconnect().Build();
```

### Health checks

* `/healthz` liveness.
* `/readyz` includes DB and Rabbit checks (tag `"ready"`).

### Env vars (compose vs k8s)

* **Web**

  * `ConnectionStrings__DefaultConnection=Host=postgres...;Database=stocksim;Username=stocksim;Password=stocksim;Pooling=true`
  * `MarketFeed__BaseUrl`:

    * compose: `http://marketfeed:8080`
    * k8s (browser needs public): `http://marketfeed.local` (used only in client-side URLs)
  * `Rabbit__Host=rabbitmq`, `Rabbit__User=stocksim`, `Rabbit__Pass=stocksim`
* **MarketFeed**

  * `ASPNETCORE_URLS=http://+:8080`

## Run Books

### Docker Compose

```bash
docker compose up -d --build
# Web: http://localhost:8080
# MarketFeed: http://localhost:8081
```

### k3d (dev)

```bash
# create
k3d cluster create --config k8s/k3d.yaml
kubectl create ns stocksim
# secrets
kubectl -n stocksim create secret generic pg-app \
  --from-literal=POSTGRES_DB=stocksim \
  --from-literal=POSTGRES_USER=stocksim \
  --from-literal=POSTGRES_PASSWORD=stocksim
kubectl -n stocksim create secret generic rabbit-app \
  --from-literal=RABBIT_USER=stocksim \
  --from-literal=RABBIT_PASSWORD=stocksim
# postgres via helm
helm repo add bitnami https://charts.bitnami.com/bitnami
helm upgrade --install -n stocksim postgres oci://registry-1.docker.io/bitnamicharts/postgresql \
  --set auth.existingSecret=pg-app --set auth.secretKeys.userPasswordKey=POSTGRES_PASSWORD \
  --set auth.username=stocksim --set auth.database=stocksim
# images
docker build -t stocksim-web:k8s -f src/StockSim.Web/Dockerfile .
docker build -t stocksim-marketfeed:k8s -f src/StockSim.MarketFeed/Dockerfile .
k3d image import stocksim-web:k8s stocksim-marketfeed:k8s -c stocksim
# apply
kubectl apply -f k8s/rabbitmq.yaml
kubectl apply -f k8s/marketfeed.yaml
kubectl apply -f k8s/web.yaml
kubectl apply -f k8s/ingress.yaml
# verify
kubectl -n stocksim get pods
iwr http://localhost/readyz -Headers @{ Host='stocksim.local' }
iwr http://localhost/readyz -Headers @{ Host='marketfeed.local' }
```

## Telemetry

- **Zipkin** exporter configured in Web/MarketFeed. Open Zipkin UI to view traces.
- **Prometheus** scrapes `/metrics`. **Grafana** uses Prometheus datasource for dashboards.

## Common Pitfalls + Fixes

- **DbContext concurrency**: “second operation in progress” → never run parallel queries on one context; use factory per method.
- **Disposed context**: don’t keep `DbContext` fields in services; materialize before leaving method; create scope per background loop.
- **Rabbit auth**: ensure `Rabbit__User`/`Rabbit__Pass` match deployment.
- **Ingress vs internal DNS**: browser must use `stocksim.local` / `marketfeed.local`; pods use `web:8080` / `marketfeed:8080`.
- **Probes**: readiness hitting a non-existent path keeps rollouts stuck; map `/readyz` or point probes to `/healthz`.
- **SignalR**: don’t force WebSockets only; allow negotiation.

## Next Steps (recommended order)

1. Order matcher v1 with unit tests.
2. Orders API for pending/active book; UI table for book.
3. React: candles + leaderboard.
4. Testcontainers integration tests.
5. GitHub Actions CI.
6. K8s prod polish (TLS, resources, PVCs).

Use this brief to resume in a new thread without context loss.