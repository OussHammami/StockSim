# StockSim — 4-minute demo script

1) Start (if not running): `docker compose up -d --build`
2) Open Dashboard (http://localhost:8080)
   - Show live quotes updating
   - Place a Buy Market order for a symbol (e.g., AAPL)
   - Watch status go Pending -> Filled; Portfolio snapshot updates
   - Place a Limit order below market; show it resting or use a crossing limit
3) Open React page (Charts)
   - Show live stream for the same symbol
4) Observability
   - Zipkin: show a trace for PlaceOrder → Persist → Outbox → SignalR
   - Grafana: show Prometheus metrics for processed orders
5) Optional k3d
   - `kubectl -n stocksim get pods`
   - Curl /readyz via Ingress host headers