#!/usr/bin/env bash
set -euo pipefail
cp -n .env.example .env || true
docker compose build
docker compose up -d
echo "Web: http://localhost:8080"
echo "Feed: http://localhost:8081"
echo "React: http://localhost:5173"
echo "RabbitMQ: http://localhost:15672"
echo "Prom: http://localhost:9090  Grafana: http://localhost:3000  Zipkin: http://localhost:9411"
