# 0001 — Bounded Contexts for StockSim

- **Status:** Accepted
- **Date:** 2025-10-27
- **Owner:** Architecture
- **Consequences:** Guides code layout, ownership, and integration boundaries.

## Decision
Establish four bounded contexts:

1. **Trading** — order intake, validation, matching policy, lifecycle.
2. **Portfolio** — cash, positions, PnL, reservations, settlements.
3. **MarketData** — quotes, subscriptions, adapters to external feeds.
4. **Identity** — users, roles, authn/z. Reuse ASP.NET Identity.

Contexts integrate via versioned **integration events** with outbox/inbox and idempotency.

## Why
Reduce coupling and clarify ownership before new features (margin, shorting, multi-asset). Current code mixes order logic, SignalR, and persistence in the Web host.

## Responsibilities

### Trading
- Accept and validate orders.
- Maintain order state: New → Accepted → PartiallyFilled → Filled/Rejected/Canceled.
- Reserve assets by emitting events to Portfolio.
- Publish integration events for UI and other contexts.

**Aggregate**
- `Order` with `OrderId`, `Symbol`, `Side`, `Quantity`, `LimitPrice`, `State`, `UserId`.

**Domain Events**
- `OrderAccepted`, `OrderPartiallyFilled`, `OrderFilled`, `OrderRejected`, `OrderCanceled`.

### Portfolio
- Cash and positions per user.
- Asset and cash reservations on order accept.
- Apply fills and settle.
- Compute realized/unrealized PnL.

**Aggregates**
- `Portfolio` (root) with `PortfolioId`, `CashAccount`, `Positions[]`.
- `Position` owned by `Portfolio`.

**Domain Events**
- `FundsReserved`, `SharesReserved`, `ReservationReleased`, `FillApplied`, `SettlementPosted`.

### MarketData
- Normalize quote ticks from any feed.
- Provide best bid/ask snapshot and stream.
- No trading or portfolio rules.

**Value Objects**
- `Quote`, `Symbol`, `Price`.

### Identity
- ASP.NET Core Identity boundary.
- User lifecycle events only if needed later.

## Context Map

```
           SignalR
[MarketData] -----> UI/React
      |
      | ACL (DTO translation)
      v
   [Trading] <----> [Portfolio]
          Published Language (integration events)
```

- Trading ↔ Portfolio: **Published Language** via a `StockSim.Contracts` package.
- MarketData → Trading: **ACL** converts external quote DTOs to internal VOs.
- UI consumes SignalR hubs from MarketData and Trading.

## Integration Events (initial set)
- From Trading: `trading.order.accepted`, `trading.order.partiallyFilled`, `trading.order.filled`, `trading.order.rejected`
- From Portfolio: `portfolio.funds.reserved`, `portfolio.shares.reserved`, `portfolio.reservation.released`, `portfolio.fill.applied`

**Envelope fields:** `id`, `type`, `occurredAt`, `source`, `subject`, `data`, `schemaVersion`, `dedupeKey`.

## Storage
- Single DB for now with separate schemas:
  - `trading.*` tables
  - `portfolio.*` tables
- Separate outbox/inbox per schema. Split DBs later if needed.

## Interfaces
- **Trading.API:** `POST /api/trading/orders`, `GET /api/trading/orders/{id}`
- **Portfolio.API:** `GET /api/portfolio/positions`, `GET /api/portfolio/cash`
- **MarketData.Hub:** `/hubs/quotes` (SignalR)
- **Trading.Hub:** `/hubs/orders` (SignalR)

## Invariants (samples)
- Trading: qty > 0; valid price for limit; valid state transitions.
- Portfolio: cash/quantity ≥ reserved + available; release reservations on terminal order states.
- MarketData: valid symbol; price ≥ 0; valid timestamp.

## Anti-corruption
- MarketData defines `FeedQuoteDto` mapped to `Quote` VO.
- No external DTOs cross into Trading/Portfolio.

## Testing Strategy
- Spec tests for Trading and Portfolio aggregates.
- Contract tests for integration events.
- Consumer tests with inbox dedupe.

## Rollout Plan
1. Create `Contracts` project for event DTOs.
2. Move current order logic toward Trading aggregate.
3. Introduce Portfolio reservations with adapters to keep UI working.
4. Extract consumers to workers per context.
5. Remove adapters after parity.

## Out of Scope Now
- Margin rules and multi-asset.
- Multiple databases.
