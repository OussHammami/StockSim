# StockSim — Architecture and Order Lifecycle (Mermaid)

This document shows:
- A high-level architecture overview.
- The end-to-end lifecycle of an order (placement → acceptance → execution → portfolio updates).
- The order state machine.

Note: This Mermaid uses broadly compatible syntax:
- `graph TB` instead of `flowchart TB`
- Class assignments via `class` (not `:::`)
- Subgraph labels as `subgraph ID [Label]`

## 1) Architecture Overview

```mermaid
graph TB
    %% Clients
    U[User / Browser]

    %% Web (Blazor Server + API)
    subgraph WEB ["Web (Blazor + API)"]
      WAPI[TradingController<br/>/api/trading/orders]
      WUI["Dashboard (Blazor)"]
      WQUIC[QuotesHubClient]
    end

    %% MarketFeed
    subgraph FEED [MarketFeed]
      QH[(SignalR QuotesHub<br/>/hubs/quotes)]
      PW[PriceWorker]
    end

    %% Trading Worker
    subgraph TW [Trading Worker]
      HQL[HubQuoteListenerHostedService]
      HSP[HubQuoteSnapshotProvider]
      TDE[TapeDrivenExecutionHostedService]
      TPE[TradePrintExecutor]
      OM[OrderMaintenanceHostedService]
      T_OUT[(Trading Outbox)]
    end

    %% Portfolio Worker
    subgraph PWK [Portfolio Worker]
      TEH[TradingEventsHandler]
      P_OUT[(Portfolio Outbox)]
    end

    %% Messaging
    MQ[(RabbitMQ)]

    %% Data
    subgraph DBs [Databases]
      TDB[(TradingDb<br/>Orders)]
      PDB[(PortfolioDb<br/>Cash/Positions)]
    end

    %% Client access
    U -->|Place / Cancel / View| WUI
    WUI -->|HTTP| WAPI

    %% Order placement path
    WAPI -->|Place order| TDB
    WAPI -->|OrderAccepted → map| T_OUT

    %% Outbox publish/consume
    T_OUT -->|publisher| MQ
    MQ -->|subscribe| TEH

    %% Portfolio apply / outbox
    TEH -->|Reserve / Apply Fill| PDB
    TEH --> P_OUT
    P_OUT -->|publisher| MQ

    %% Quotes flow
    PW --> QH
    HQL -->|Subscribe| QH
    HQL --> HSP

    %% Tape-driven execution
    TDE -->|Consume TradePrint| TPE
    TPE -->|Load/Update Orders| TDB
    TPE -->|OrderFillApplied / OrderCompleted → map| T_OUT

    %% UI reads
    WUI -->|HTTP| WAPI
    WAPI -->|GET orders| TDB
    WAPI -->|GET positions/summary| PDB

    %% Styling classes
    classDef client fill:#fdf6e3,stroke:#b58900,color:#333;
    classDef ui fill:#e6f7ff,stroke:#1890ff,color:#333;
    classDef svc fill:#fffbe6,stroke:#faad14,color:#333;
    classDef core fill:#fff0f6,stroke:#eb2f96,color:#333;
    classDef db fill:#f6ffed,stroke:#52c41a,color:#333;
    classDef hub fill:#f0f5ff,stroke:#2f54eb,color:#333;
    classDef bus fill:#fff1f0,stroke:#f5222d,color:#333;
    classDef lib fill:#f9f0ff,stroke:#722ed1,color:#333;

    class U client
    class WUI ui
    class WAPI svc
    class WQUIC lib
    class QH hub
    class PW svc
    class HQL svc
    class HSP lib
    class TDE svc
    class TPE core
    class OM svc
    class T_OUT db
    class TEH svc
    class P_OUT db
    class MQ bus
    class TDB db
    class PDB db
```

---

## 2) Order Lifecycle (end-to-end)

```mermaid
sequenceDiagram
    autonumber
    actor User as User (Browser)
    participant UI as Web UI (Dashboard)
    participant API as TradingController (Web API)
    participant SVC as OrderService (Application)
    participant ORD as Order Aggregate (Domain)
    participant TDB as TradingDb
    participant MAP as IntegrationEventMapper
    participant T_OUT as Trading Outbox
    participant MQ as RabbitMQ
    participant P_C as TradingEventsHandler (Portfolio Worker)
    participant PORT as Portfolio Aggregate (Domain)
    participant PDB as PortfolioDb

    %% Place
    User->>UI: Submit Place Order (symbol, side, type, qty, limit?)
    UI->>API: POST /api/trading/orders
    API->>SVC: PlaceAsync(command)
    SVC->>ORD: CreateMarket/CreateLimit
    ORD-->>SVC: Order (state=New)
    SVC->>ORD: Accept()
    ORD-->>SVC: DomainEvent: OrderAccepted

    %% Persist + Outbox
    SVC->>TDB: AddAsync(order) + Save
    SVC->>MAP: Map(OrderAccepted)
    MAP-->>SVC: IntegrationEvent "trading.order.accepted"
    SVC->>T_OUT: Write(outbox rows)

    %% Publish + Reserve
    T_OUT-->>MQ: Publish "trading.order.accepted"
    MQ-->>P_C: Consume event
    P_C->>PORT: ReserveFunds / ReserveShares
    PORT-->>P_C: DomainEvents (FundsReserved / SharesReserved)
    P_C->>PDB: Save changes (reservations)

    Note over UI,SVC: Order is now Accepted and visible in the UI list.

    %% Execution (Tape-driven)
    participant QUO as QuotesHub (MarketFeed)
    participant HQL as HubQuoteListener
    participant HSP as SnapshotProvider
    participant TDE as TapeDrivenExecutionHostedService
    participant TPE as TradePrintExecutor

    QUO-->>HQL: live quotes
    HQL->>HSP: cache snapshots

    TDE-->>TPE: on TradePrint(symbol, price, qty)
    TPE->>TDB: GetOpenBySymbol(symbol) [tracked]
    TPE->>ORD: ApplyFill(quantity, price)
    ORD-->>TPE: DomainEvent: OrderFillApplied (each tranche)
    alt RemainingQuantity == 0
        ORD-->>TPE: DomainEvent: OrderCompleted (terminal)
    end
    TPE->>MAP: Map(fill/completed)
    TPE->>TDB: Save changes (state PartiallyFilled/Filled)
    TPE->>T_OUT: Write(outbox rows)

    %% Portfolio mutation on fills
    T_OUT-->>MQ: Publish "trading.order.fillApplied"
    MQ-->>P_C: Consume fillApplied
    P_C->>PORT: ApplyFill(orderId, side, symbol, qty, price)
    PORT-->>P_C: DomainEvent: FillApplied
    P_C->>PDB: Save (positions + cash)
    P_C->>P_OUT: Write outbox (portfolio.fill.applied)    
```

---

## 3) Order State Machine

```mermaid
stateDiagram-v2
    [*] --> New
    New --> Accepted: Accept()
    Accepted --> PartiallyFilled: ApplyFill(q<pending)
    PartiallyFilled --> PartiallyFilled: ApplyFill(more)
    Accepted --> Filled: ApplyFill(q=remaining)
    PartiallyFilled --> Filled: ApplyFill(q=remaining)
    Accepted --> Canceled: Cancel(reason)
    PartiallyFilled --> Canceled: Cancel(reason)
    New --> Rejected: Reject(reason)
    Accepted --> Rejected: Reject(reason)
    PartiallyFilled --> Rejected: Reject(reason)
    Filled --> [*]
    Canceled --> [*]
    Rejected --> [*]
```

---