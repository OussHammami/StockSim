@echo off
setlocal
if not exist ".env" if exist ".env.example" copy ".env.example" ".env" >nul
docker compose build
if errorlevel 1 exit /b %errorlevel%
docker compose up -d
echo Web: http://localhost:8080 & echo Feed: http://localhost:8081 & echo React: http://localhost:5173
echo RabbitMQ: http://localhost:15672  Prom: http://localhost:9090  Grafana: http://localhost:3000  Zipkin: http://localhost:9411
endlocal
