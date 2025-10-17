# Azure Container Apps (ACA) deployment

This guide sets up an ACA environment and deploys StockSim services using images from GHCR.

## Prerequisites

- Azure subscription and permissions to create:
  - Resource group
  - Log Analytics workspace
  - Container Apps environment and apps
- GitHub repository secrets:
  - `AZURE_CREDENTIALS`: JSON for a federated or password-based service principal with at least `Contributor` on the resource group.

Example service principal JSON (Client secret based):
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

Tip: Prefer OIDC (federated credentials) for CI. Use the [azure/login](https://github.com/azure/login) docs to set up a federated identity and avoid secrets.

## Images

Make sure your GHCR images exist (build workflow should have pushed these):
- `ghcr.io/<owner>/stocksim-web:latest`
- `ghcr.io/<owner>/stocksim-marketfeed:latest`
- `ghcr.io/<owner>/stocksim-react:latest` (optional first pass; see React note below)

If GHCR repos are private, the ACA environment will need registry auth. Easiest path is to make images public for dev; otherwise, create a registry credential in each Container App:
```bash
az containerapp registry set \
  -g <rg> -n web \
  --server ghcr.io \
  --username <github-username> \
  --password <github-PAT-with-packages-read>
```

## React and MarketFeed URL

React’s MarketFeed URL is compiled at build-time. The ACA MarketFeed FQDN is only known at deploy-time, so the shipped `latest` image will not point to it by default.

Options:
- Quickstart (recommended): Deploy Web + MarketFeed now, skip React (`deploy_react=false`). We’ll add a small “runtime config” step in React next iteration so the URL can be injected at runtime.
- If you deploy React now, UI loads but charts won’t connect to MarketFeed until we implement runtime config.

## Run the workflow

1. Go to Actions → “Deploy - Azure Container Apps (ACA)” → Run workflow.
2. Fill in:
   - `subscription_id` (required)
   - `resource_group` (e.g., `rg-stocksim-dev`)
   - `location` (e.g., `westeurope`)
   - `env_name` (e.g., `aca-stocksim-dev`)
   - `image_tag` (usually `latest` or a commit SHA)
   - `deploy_react` = `false` for the first run

The job outputs public endpoints:
- Web: `https://<fqdn>`
- MarketFeed: `https://<fqdn>`
- Postgres & RabbitMQ internal FQDNs are plumbed into Web via environment variables.

## Smoke test

- Open Web URL: should render the Blazor app.
- MarketFeed health: `curl https://<marketfeed-fqdn>/healthz` should return 200.
- Place a market order in the Web UI; it should fill using the latest price coming from MarketFeed’s SignalR hub.

## Next iteration (React runtime config)

We’ll add a tiny runtime-config mechanism to the React image so we can pass `VITE_MARKETFEED_URL` at container start (instead of build time). Steps:
- Serve an `env.js` from Nginx with `window.__env = { VITE_MARKETFEED_URL: '...' }`.
- Update `useQuotes` to prefer `window.__env?.VITE_MARKETFEED_URL`.
- Add an entrypoint to render `env.js` from an environment variable.

With that, ACA can set a container environment variable for React and your charts page will use the deployed MarketFeed FQDN automatically.

## Production notes

- Postgres: Use Azure Database for PostgreSQL Flexible Server. Update `ConnectionStrings__DefaultConnection` accordingly.
- RabbitMQ: Either keep Rabbit as ACA app (backed by Azure Files volume) or refactor to a managed broker pattern.
- Ingress: Add a custom domain and TLS certs via ACA’s managed certificates.
- Observability: You already expose Prometheus/metrics; point Grafana to MarketFeed/Web metrics or use Azure Monitor/Log Analytics for logs and traces.
