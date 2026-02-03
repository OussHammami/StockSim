# StockSim runtime configuration

This document lists the runtime configuration contract for each service:
- which keys are required
- which keys are secrets
- how keys map to environment variables in containers/cloud

## Configuration rules (applies everywhere)
- Do not commit secrets to git.
- Local dev uses .NET User Secrets.
- Containers/cloud use environment variables (and later: a secret store).
- Environment variables use `__` for nesting (e.g., `Rabbit__Host` == `Rabbit:Host`).

---

## Services

### 1) web (Blazor Server)
Purpose: UI + API + SignalR orders hub.

**Non-secret config**
- `ASPNETCORE_ENVIRONMENT` (recommended explicit)
- `ASPNETCORE_URLS` (e.g., `http://+:8080`)
- `MarketFeed__BaseUrl` (e.g., `http://marketfeed:8081`)
- `Rabbit__Host`
- `Rabbit__Port`
- `Rabbit__Queue`
- `Rabbit__Durable`
- `Cors__AllowedOrigins__0..n` (if using env var array style; optional if you keep in appsettings)

**Secrets**
- `ConnectionStrings__AuthDb`
- `ConnectionStrings__TradingDb`
- `ConnectionStrings__PortfolioDb`
- `Rabbit__User`
- `Rabbit__Pass`

Health endpoints
- `/healthz`
- `/readyz`

---

### 2) marketfeed (quotes + SignalR)
Purpose: streams quotes on SignalR hub.

**Non-secret config**
- `ASPNETCORE_ENVIRONMENT`
- `ASPNETCORE_URLS` (e.g., `http://+:8081`)
- `Cors__AllowedOrigins__0..n` (optional)

**Secrets**
- none (current implementation generates prices locally)

Health endpoints
- `/healthz`
- `/readyz`

---

### 3) trading-worker
Purpose: consumes events, writes trading state, publishes outbox events.

**Non-secret config**
- `DOTNET_ENVIRONMENT`
- `MarketFeed__BaseUrl`
- `Rabbit__Host`
- `Rabbit__Port`
- `Rabbit__Queue`
- `Rabbit__Durable`

**Secrets**
- `ConnectionStrings__TradingDb`
- `Rabbit__User`
- `Rabbit__Pass`

---

### 4) portfolio-worker
Purpose: consumes events, writes portfolio state, publishes outbox events.

**Non-secret config**
- `DOTNET_ENVIRONMENT`
- `MarketFeed__BaseUrl`
- `Rabbit__Host`
- `Rabbit__Port`
- `Rabbit__Queue`
- `Rabbit__Durable`

**Secrets**
- `ConnectionStrings__PortfolioDb`
- `Rabbit__User`
- `Rabbit__Pass`

---

### 5) react (static site served by nginx)
Purpose: frontend SPA.

**Runtime config (generated into /env.js at container startup)**
- `VITE_MARKETFEED_URL` (example: `http://localhost:8081` or `https://<prod-domain>`)

Notes
- Do not rely on build-time `import.meta.env` for environment-specific URLs.
- `/env.js` is served with `Cache-Control: no-store`.

---

## Local dev vs containers
- Local dev (`dotnet run`):
  - secrets via .NET User Secrets
  - environment should be Development for workers: `DOTNET_ENVIRONMENT=Development`

- Containers (`docker compose` / cloud):
  - secrets via env vars (and later: secret store)
  - validate Production mode regularly (no User Secrets)

---

## Example: container env vars (minimal)
web:
- ConnectionStrings__AuthDb=...
- ConnectionStrings__TradingDb=...
- ConnectionStrings__PortfolioDb=...
- MarketFeed__BaseUrl=http://marketfeed:8081
- Rabbit__Host=rabbitmq
- Rabbit__Port=5672
- Rabbit__User=...
- Rabbit__Pass=...
- Rabbit__Queue=stocksim.events
- Rabbit__Durable=true

trading-worker:
- ConnectionStrings__TradingDb=...
- MarketFeed__BaseUrl=http://marketfeed:8081
- Rabbit__Host=rabbitmq
- Rabbit__Port=5672
- Rabbit__User=...
- Rabbit__Pass=...
- Rabbit__Queue=stocksim.events
- Rabbit__Durable=true

portfolio-worker:
- ConnectionStrings__PortfolioDb=...
- MarketFeed__BaseUrl=http://marketfeed:8081
- Rabbit__Host=rabbitmq
- Rabbit__Port=5672
- Rabbit__User=...
- Rabbit__Pass=...
- Rabbit__Queue=stocksim.events
- Rabbit__Durable=true

react:
- VITE_MARKETFEED_URL=http://localhost:8081
