# StockSim – Portfolio Trading Simulator

Blazor Server + MudBlazor UI, React/Vite charts, real‑time quotes via SignalR, DDD bounded contexts (Trading, Portfolio), RabbitMQ (outbox pattern), PostgreSQL (separate databases per context), full observability (OpenTelemetry Collector, Prometheus, Grafana, Zipkin). Docker Compose and optional Kubernetes manifests for local/demo deployment.

---

## Contents
- [Quickstart](#quickstart-5-minutes)
- [Ports](#ports)
- [Environment & Databases](#environment--databases)
- [Architecture](#architecture)
- [Demo Script](#demo-script-90-seconds)
- [Testing & Coverage](#testing--coverage)
- [Observability](#observability)
- [Troubleshooting](#troubleshooting)
- [Kubernetes](#kubernetes)
- [License](#license)

---

## Quickstart (≈5 minutes)

Prereqs: Docker Desktop 4.x+

### 1) Create .env
```bash
cp .env.example .env
```
Adjust credentials only if needed.

### 2) Multi‑database initialization (Option B)
We use separate physical Postgres databases: `stocksim_auth`, `stocksim_trading`, `stocksim_portfolio` plus a base `stocksim` database (created by `POSTGRES_DB`).

- Make sure docker-compose mounts the Postgres init directory:
  ```
  volumes:
    - pgdata:/var/lib/postgresql/data
    - ./ops/postgres-init:/docker-entrypoint-initdb.d:ro
  ```
- The init SQL in `ops/postgres-init/` runs only on a fresh volume. If you change or add init SQL, remove the existing volume to re‑initialize:
  ```bash
  docker compose down
  docker volume rm $(docker volume ls -q | grep pgdata) || true
  ```

### 3) Start the stack
Windows:
```powershell
scripts\dev-up.cmd
```
macOS/Linux:
```bash
chmod +x scripts/dev-up.sh scripts/dev-down.sh
./scripts/dev-up.sh
```

### 4) Open UIs
- Web UI: http://localhost:8080
- Market Feed: http://localhost:8081
- React Charts: http://localhost:5173
- RabbitMQ UI: http://localhost:15672 (stocksim/stocksim by compose defaults)
- Grafana: http://localhost:3000 (admin/admin by default image)
- Prometheus: http://localhost:9090
- Zipkin: http://localhost:9411

### 5) Login (demo seed)
If `DEMO__SEED=true`, first run seeds demo users & data:
- Admin: `admin@demo.local` / `Pass123$`
- Trader: `trader@demo.local` / `Pass123$`

---

## Ports

| Component      | URL / Host:Port        | Notes                                   |
| -------------- | ----------------------- | ----------------------------------------|
| Web (Blazor)   | http://localhost:8080   | Internal container: 8080                 |
| Market Feed    | http://localhost:8081   | SignalR quotes hub                       |
| React Charts   | http://localhost:5173   | Vite build served by nginx               |
| PostgreSQL     | localhost:5433          | Host mapped → container port 5432        |
| RabbitMQ UI    | http://localhost:15672  | AMQP on 5672                             |
| Prometheus     | http://localhost:9090   | Scrapes otelcol and otelcol-internal     |
| Grafana        | http://localhost:3000   | Dashboards auto-provisioned              |
| Zipkin         | http://localhost:9411   | Traces via OTLP -> collector             |

Important: Inside containers, always connect to Postgres on `Host=postgres;Port=5432`.

---

## Environment & Databases

Key variables (see `.env.example`):
```
ConnectionStrings__AuthDb=Host=postgres;Port=5432;Database=stocksim_auth;Username=stocksim;Password=stocksim
ConnectionStrings__TradingDb=Host=postgres;Port=5432;Database=stocksim_trading;Username=stocksim;Password=stocksim
ConnectionStrings__PortfolioDb=Host=postgres;Port=5432;Database=stocksim_portfolio;Username=stocksim;Password=stocksim

MarketFeed__BaseUrl=http://marketfeed:8081
WEB__PUBLIC_URL=http://localhost:8080

# React build-time
VITE_QUOTES_HUB_URL=http://localhost:8081/hubs/quotes
VITE_API_BASE_URL=http://localhost:8080

# OTel → Collector
OTEL_EXPORTER_OTLP_ENDPOINT=http://otelcol:4317

# Demo seed
DEMO__SEED=true
DEMO__AdminEmail=admin@demo.local
DEMO__AdminPassword=Pass123$
DEMO__UserEmail=trader@demo.local
DEMO__UserPassword=Pass123$
```

If you see `3D000: database ... does not exist`, the init script likely didn’t run because an old `pgdata` volume was reused. Remove the volume and restart (see Quickstart step 2).

---

## Architecture

Bounded contexts:
- Trading (order lifecycle)
- Portfolio (positions, cash, reservations)
- MarketFeed (quotes hub & simulated feed)
- Auth/Web (Identity, Blazor UI, API façade)

Layers:
- Domain: entities, value objects, domain events
- Application: commands/services (e.g., `OrderService`)
- Infrastructure: EF Core repos, Outbox/Inbox, persistence config
- Workers: Trading & Portfolio background processors consuming RabbitMQ
- Web: Blazor Server + API endpoints + SignalR integration
- React: charts and extended UI

Data:
- Separate Postgres databases per context (Option B)
- EF Core migrations applied on startup

Messaging & real‑time:
- RabbitMQ for domain → integration events (Outbox pattern)
- SignalR quotes hub (`/hubs/quotes`), transport messages shaped like `QuoteMsg(Symbol, Bid, Ask, Last?, Ts)`

Orders:
- Dashboard shows recent orders via `GET /api/trading/orders` (per user), including state, fills, type, limit, and CreatedAt.

Quotes UI:
- Color‑coded price change (green/red) with arrows
- Selectable rows that prefill the trade Symbol
- (Optional) display Bid/Ask/Spread for liquidity

---

## Demo Script (90 seconds)
1. Open dashboard; observe live quotes updating.
2. Click a symbol row — trade form auto‑fills.
3. Place a Market order — see status transitions (Pending → Accepted → Partial/Filled).
4. Place a Limit order below market — it rests until crossed.
5. Open Grafana (StockSim Overview) — observe CPU and ASP.NET requests/sec.
6. Open Zipkin — trace shows Place → Persist → Outbox → Hub broadcast.

---

## Testing & Coverage

- Unit tests: Domain (aggregates/VOs), Application (services)
- Integration: EF Core repositories with Testcontainers (Postgres)
- Web/API integration tests (WebApplicationFactory)
- E2E (Playwright) available under `e2e/`

Run locally:
```bash
dotnet test --collect:"XPlat Code Coverage"
# optional merge + HTML report (requires reportgenerator installed)
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:coverage -reporttypes:"HtmlInline;TextSummary"
```
CI publishes test results and a coverage report artifact.

---

## Observability

- OpenTelemetry: ASP.NET Core, HTTP, EF Core, runtime
- Collector receives OTLP exports and:
  - sends traces to Zipkin at http://localhost:9411
  - re-exposes app metrics at otelcol:9464 for Prometheus
  - exposes internal collector metrics at otelcol:8888 for Prometheus
- Grafana dashboards are auto-provisioned from `ops/grafana/dashboards/`
  - Example panels: `rate(process_cpu_seconds_total[1m])`, `aspnetcore_requests_per_second`

---

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `3D000: database ... does not exist` | Init SQL didn’t run (existing volume) | `docker compose down -v` then up; ensure `ops/postgres-init` is mounted |
| Connection refused to Postgres (inside containers) | Using host port 5433 internally | Use `Port=5432` for in‑container connections |
| Orders not showing | API/auth issue | Check Web logs; verify seed users; confirm `GET /api/trading/orders` |
| Quotes empty | Hub or feed down | Check MarketFeed logs; verify `VITE_QUOTES_HUB_URL` and container names |
| RabbitMQ unhealthy | Startup delay | `docker compose logs rabbitmq` and wait for healthcheck |
| Grafana panels blank | Prometheus scrape failing | Check targets: Prometheus → Status → Targets |
| Missing traces | OTEL endpoint mismatch | Verify `OTEL_EXPORTER_OTLP_ENDPOINT` in containers |

Reset everything (fresh local run):
```bash
docker compose down -v
rm -rf src/**/bin src/**/obj
docker compose up -d --build
```

---

## Kubernetes

Manifests under `K8s/` (web, marketfeed, rabbitmq, ingress, k3d cluster) provide a starting point. For multi‑database in K8s, add a Postgres deployment with an init Job (or use an operator) to create the additional databases.

Example (k3d):
```bash
k3d cluster create stocksim -c K8s/k3d.yaml
kubectl apply -f K8s/rabbitmq.yaml -f K8s/marketfeed.yaml -f K8s/web.yaml -f K8s/ingress.yaml
```

---

## License

MIT. See `LICENSE.txt`.

---

## ADRs

See `docs/adr/`:
- 0001 — Bounded Contexts for StockSim (Accepted)
