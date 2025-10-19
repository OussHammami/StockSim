# scripts/dev-up.sh
#!/usr/bin/env bash
set -euo pipefail
cp -n .env.example .env || true
docker compose build
docker compose up -d
echo "Web:       http://localhost:8080"
echo "MarketFeed:http://localhost:8081"
echo "RabbitMQ:  http://localhost:15672  guest/guest"
echo "Grafana:   http://localhost:3000    admin/admin"
echo "Zipkin:    http://localhost:9411"
