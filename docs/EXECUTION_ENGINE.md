# Execution Engine Overview

The execution engine (trading worker) processes incoming quotes and open orders:

1. Internal Crossing: Matches opposing resting limit orders in the in-memory OrderBook (highest bid vs lowest ask).
2. Quote-Based Execution: If no internal cross, marketable orders (Market or crossing Limit) execute against external quote (Bid/Ask).
3. Time-In-Force:
   - IOC: executes immediately for available quantity; cancels remainder.
   - FOK: requires full remaining quantity in one attempt; otherwise no fill.
   - DAY: expires at end of UTC day.
   - GTC: no automatic expiration.
4. Slippage: Simple linear model adjusts fill price based on order size vs configured scale.
5. Reservation: Funds or shares reserved on OrderAccepted to ensure portfolio integrity at fill time.
6. Telemetry: Metrics (orders scanned, filled, quantity, latency) exported via OpenTelemetry.

Future roadmap includes multi-level depth, advanced slippage, risk checks, and persistent order book snapshots.