# StockSim Architecture

## System Overview

```mermaid
flowchart LR
subgraph Browser
  UI[Blazor Server UI]:::ui
  React[React Charts]:::ui
end

subgraph WebApp[StockSim.Web]
  HubOrders[SignalR Orders Hub]
  OrderPublisher[OrderPublisher]
  OutboxDispatcher[Outbox Dispatcher]
  Services[App Services]
end

subgraph MarketFeed[StockSim.MarketFeed]
  HubQuotes[SignalR Quotes Hub]
  PriceSim[Price Simulator]
end

subgraph Infra[Infrastructure]
  DB[(PostgreSQL)]
  Rabbit[(RabbitMQ)]
end

UI -- SignalR --> HubOrders
React -- SignalR --> HubQuotes

UI <-- SignalR (orders, snapshot) --- HubOrders
UI -. REST (place order) .-> Services
Services -- EF Core --> DB
Services -- publish --> OrderPublisher --> Rabbit

Rabbit -- subscribe --> Consumer[[OrderConsumer/Matcher]]
Consumer -- EF Core + Outbox --> DB
OutboxDispatcher -- SignalR events --> HubOrders

PriceSim --> HubQuotes
WebApp -- OTEL Traces --> Zipkin[(Zipkin)]
MarketFeed -- OTEL Traces --> Zipkin
WebApp -. metrics .-> Prometheus[(Prometheus)]
MarketFeed -. metrics .-> Prometheus
Grafana[(Grafana)] --> Prometheus

classDef ui fill:#e3f2fd,stroke:#90caf9,color:#0d47a1;
```

## Place Order Sequence (Minimal Matcher)

```mermaid
sequenceDiagram
  actor User as User
  participant Blazor as StockSim.Web (Blazor)
  participant DB as PostgreSQL
  participant MQ as RabbitMQ
  participant Consumer as OrderConsumer/Matcher
  participant Outbox as OutboxDispatcher
  participant Hub as SignalR Orders Hub

  User->>Blazor: PlaceOrder(Market/Limit, qty, symbol)
  Blazor->>DB: Persist Order (Pending)
  Blazor->>MQ: Publish OrderCommand
  MQ-->>Consumer: Deliver OrderCommand
  Consumer->>DB: Read latest Quote (or cached)
  Consumer->>DB: Create Fill at Quote price; update Remaining/Status
  Consumer->>DB: Append Outbox events (OrderUpdated/FillCreated)
  Outbox->>DB: Read new Outbox events (idempotent)
  Outbox->>Hub: Broadcast updates to clients
  Hub-->>User: UI shows status change + portfolio refresh
```

## Notes
- Minimal matcher: Market and crossing Limit fill at the latest quote. Non-crossing Limits rest in the book.
- Outbox ensures idempotent SignalR notifications.
- In Kubernetes (k3d), browser connects via Ingress hosts: stocksim.local (orders) and marketfeed.local (quotes).