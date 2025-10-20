@echo off
setlocal
if not exist ".env" (
  if exist ".env.example" copy ".env.example" ".env" >nul
)
docker compose build
if errorlevel 1 exit /b %errorlevel%
docker compose up -d
echo.
echo Web:        http://localhost:8080
echo MarketFeed: http://localhost:8081
echo RabbitMQ:   http://localhost:15672
echo Prometheus: http://localhost:9090
echo Grafana:    http://localhost:3000
echo Zipkin:     http://localhost:9411
endlocal
