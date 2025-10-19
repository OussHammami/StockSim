#!/bin/sh
# seed.sh - waits until the AspNetUsers table exists (i.e. Web's migrations have run), then runs seed.sql
set -eu

PGHOST=${PGHOST:-pg}
PGPORT=${PGPORT:-5432}
PGUSER=${PGUSER:-stocksim}
PGDATABASE=${PGDATABASE:-stocksim}
export PGPASSWORD=${PGPASSWORD:-stocksim}

echo "Waiting for database to accept connections..."
until pg_isready -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" >/dev/null 2>&1; do
  sleep 1
done
echo "Postgres is ready. Waiting for schema..."

# Wait until AspNetUsers table exists (created by EF migrations)
COUNT=0
while true; do
  EXISTS=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -tAc "SELECT to_regclass('public.\"AspNetUsers\"');")
  if [ -n "$EXISTS" ] && [ "$EXISTS" != " " ]; then
    echo "Schema detected."
    break
  fi
  COUNT=$((COUNT+1))
  if [ "$COUNT" -gt 120 ]; then
    echo "Timed out waiting for schema (AspNetUsers) after 120s" >&2
    exit 1
  fi
  sleep 1
done

echo "Running seed SQL..."
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" -f /seed/seed.sql
echo "Seeding completed."
# keep container alive for logs / inspection for a short time then exit
sleep 2