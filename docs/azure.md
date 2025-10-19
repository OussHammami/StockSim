# Azure Container Apps

## Services
web: 8080 public
marketfeed: 8080 public
postgres: 443 internal → 5432 container (plain TCP)
rabbitmq: 443 internal → 5672 container (plain TCP)

## App settings (web)
ConnectionStrings__DefaultConnection=Host=<pg-fqdn>;Port=443;Database=stocksim;Username=stocksim;Password=stocksim;Ssl Mode=Disable;Timeout=15;Command Timeout=30;Keepalive=30;Tcp Keepalive=true
Rabbit__Host=<rabbit-fqdn>
Rabbit__Port=443
MarketFeed__BaseUrl=https://<marketfeed-fqdn>
ASPNETCORE_URLS=http://0.0.0.0:8080

## Health
/healthz  liveness
/readyz   readiness (db + rabbit)
/metrics  Prometheus

# DNS/TCP from web container
nc -vz <pg-fqdn> 443
# If using psql locally:
psql "host=<pg-fqdn> port=443 dbname=stocksim user=stocksim password=stocksim sslmode=disable"

# Logs
az containerapp logs show -n web -g <rg> --follow
# Watch for Npgsql SSL handshake errors → fix connection string to Ssl Mode=Disable
