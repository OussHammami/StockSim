# StockSim – Portfolio Trading Simulator

Blazor Server app with Identity + MudBlazor. Market feed via SignalR. RabbitMQ outbox. PostgreSQL with EF Core. Observability with Prometheus, Grafana, Zipkin. Docker Compose for local demo.

---

## Quickstart (≈5 minutes)

**Prereqs:** Docker Desktop 4.x+

### Start (Windows)

```powershell
# from repo root
scripts\\dev-up.cmd
```

### Start (macOS/Linux)

```bash
chmod +x scripts/dev-up.sh scripts/dev-down.sh  # first time only
./scripts/dev-up.sh
```

Open:

* Web UI: [http://localhost:8080](http://localhost:8080)
* Market Feed: [http://localhost:8081](http://localhost:8081)
* React Charts: [http://localhost:5173](http://localhost:5173)
* RabbitMQ UI: [http://localhost:15672](http://localhost:15672)  (guest/guest)
* Grafana: [http://localhost:3000](http://localhost:3000)        (admin/admin by default image)
* Prometheus: [http://localhost:9090](http://localhost:9090)
* Zipkin: [http://localhost:9411](http://localhost:9411)

Login:

* Admin: `admin@demo.local` / `Pass123$`
* Trader: `trader@demo.local` / `Pass123$`

> First run seeds identity and demo portfolio when `DEMO__SEED=true` (default in `.env.example`).

---

## Demo script (90 seconds)

1. Dashboard: confirm live quotes update.
2. Buy Market (e.g., AAPL). Watch status: Pending → Filled/Partial.
3. Place a Limit below market; show resting order or crossing partial fill.
4. Open Order Book and Candles.
5. Open Grafana → “StockSim Overview” (CPU seconds rate, ASP.NET requests/sec).
6. Open Zipkin; find trace for Place Order → Persist → Outbox → Hub push.

---

## Ports

| Component    | URL                      |
| ------------ | ------------------------ |
| Web          | `http://localhost:8080`  |
| Market Feed  | `http://localhost:8081`  |
| React Charts | `http://localhost:5173`  |
| PostgreSQL   | `localhost:5432`         |
| RabbitMQ UI  | `http://localhost:15672` |
| Prometheus   | `http://localhost:9090`  |
| Grafana      | `http://localhost:3000`  |
| Zipkin       | `http://localhost:9411`  |

--------------|--------------------------|
| Web          | `http://localhost:8080` |
| Market Feed  | `http://localhost:8081` |
| PostgreSQL   | `localhost:5432`         |
| RabbitMQ UI  | `http://localhost:15672` |
| Prometheus   | `http://localhost:9090`  |
| Grafana      | `http://localhost:3000`  |
| Zipkin       | `http://localhost:9411`  |

---

## One-click local stack

```bash
# up
./scripts/dev-up.sh

# down and remove volumes
./scripts/dev-down.sh
```

### Environment

Copy and edit as needed:

```bash
cp .env.example .env
```

Key variables:

```
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=stocksim;Username=stocksim;Password=stocksim
MarketFeed__BaseUrl=http://marketfeed:8081
Rabbit__Host=rabbitmq
Rabbit__Port=5672

# React build-time
VITE_QUOTES_HUB_URL=http://localhost:8081/hubs/quotes
VITE_API_BASE_URL=http://localhost:8080

DEMO__SEED=true
DEMO__AdminEmail=admin@demo.local
DEMO__AdminPassword=Pass123$
DEMO__UserEmail=trader@demo.local
DEMO__UserPassword=Pass123$
```

---

## Observability

* **Prometheus:** scrapes Web and MarketFeed.
* **Grafana:** provisioned dashboards from `ops/grafana/dashboards/`.

  * `stocksim.json` includes:

    * `rate(process_cpu_seconds_total[1m])`
    * `aspnetcore_requests_per_second`
* **Zipkin:** view traces from order publish → outbox → hub.

---

## Tech outline

* **Frontend:** Blazor Server + MudBlazor. Live quotes via SignalR.
* **Backend:** ASP.NET Core, EF Core (PostgreSQL), Identity.
* **Messaging:** RabbitMQ. Outbox pattern to hub.
* **Testing:** xUnit unit tests; integration with Testcontainers; Playwright e2e.
* **CI/CD:** GitHub Actions (build, test, docker build).

---

## Troubleshooting

* Docker ports busy → change external bindings in `docker-compose.yml`.
* Pending migrations → the app auto-applies; check app logs.
* CORS or hub errors → verify `MarketFeed__BaseUrl` and container names.
* RabbitMQ unhealthy → wait for healthcheck; check `docker compose logs rabbitmq`.
* Grafana shows empty panels → confirm Prometheus scrape and targets are UP.

---

## License

MIT. See `LICENSE.txt`.
