```text
StockSim — Local Docker Compose demo

Overview
- This compose file builds the services from the repository and runs:
  - Postgres (pg)
  - RabbitMQ (rabbitmq) with management UI (15672)
  - MarketFeed (ASP.NET app)
  - Web (Blazor Server app)
  - React (NGINX serving the built React app)
  - A seed job that waits for migrations and inserts demo data

Files added
- docker-compose.yml
- .env.example
- demo/seed/seed.sh
- demo/seed/seed.sql

Quick-start (one-liners)

1) Copy the example env (optional)
   cp .env.example .env

2) Start everything (build local images and start)
   docker compose up --build -d

3) Watch logs (all)
   docker compose logs -f

4) After the web container applies migrations the "seed" service will run automatically and insert demo portfolio/positions/orders for user 'demo-user'. You will see "Seeding completed." in the seed logs.

Useful endpoints (defaults)
- Web (Blazor): http://localhost:8080
- MarketFeed API: http://localhost:8081
- React UI: http://localhost:3000
- Postgres DB: localhost:5432 (user=stocksim / pass=stocksim / db=stocksim)
- RabbitMQ management UI: http://localhost:15672 (user=stocksim / pass=stocksim)

If the apps fail to start or Web crashes
- Run:
  docker compose logs web --tail=200
  docker compose logs pg --tail=200
- If Web crashes while applying migrations, check pg logs to ensure migrations can run. The seed job waits for the AspNetUsers table before applying seed.sql.

Notes about demo user and authentication
- The seed script creates portfolio/positions/orders for user id 'demo-user' but does NOT create a corresponding ASP.NET Identity user with a password. You can:
  - Register a new account in the Web UI (Register) and note the returned user id (for advanced debugging)
  - Or modify demo/seed/seed.sql to use an existing user id you created manually.

Customizing
- Change ports or credentials in .env before running docker compose up to override defaults.
- If you already pushed images to a registry and want to reuse them, change the build sections in docker-compose.yml to image:ghcr.io/... and remove the build context.

Troubleshooting
- If seed fails with "Timed out waiting for schema", that means Web didn't create the schema fast enough. Restart the seed service:
  docker compose up --force-recreate --no-deps -d seed
- To enter the web container for debugging:
  docker compose exec web sh
- To run ad-hoc SQL against the DB from your host:
  psql "host=localhost port=5432 user=${POSTGRES_USER:-stocksim} password=${POSTGRES_PASSWORD:-stocksim} dbname=${POSTGRES_DB:-stocksim}"

That’s it — once you add these files to the repo run docker compose up --build -d and you should have a local demo running with streaming MarketFeed and the React charts visible at http://localhost:3000.
```