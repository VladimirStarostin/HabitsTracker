# HabitsTracker: docker-compose for tests/CI

## What this brings up
- **db**: Postgres 15 with init scripts from `./pg_db/initdb` (creates `habits_db`, `tracking_db`, `users_db`).
- **habits-service** (5000/http, 5001/grpc)
- **habits-tracking-service** (5005/http, 5006/grpc)
- **user-service** (5010/http, 5011/grpc)

All services share the network `habitstracker-net`. Services wait for Postgres to become healthy.

## Files
- `docker-compose.tests.yml` — the stack
- `.env` — environment (fill from `.env.sample`)
- `pg_db/initdb/create-multi.sql` — creates DBs on first run

## Usage
```bash
cp .env.sample .env
docker compose -f docker-compose.tests.yml up -d --build
# logs
docker compose -f docker-compose.tests.yml logs -f db
docker compose -f docker-compose.tests.yml logs -f habits-service
```

## CI tip
Run:
```bash
docker compose -f docker-compose.tests.yml up -d --build
# Wait for health (optional if your test runner already retries)
docker compose -f docker-compose.tests.yml ps
# Then run your tests that connect to the service ports.
```

## Connection strings (defaults)
- Habits:   `Host=db;Port=5432;Username=$PG_USER;Password=$PG_PASSWORD;Database=habits_db`
- Tracking: `Host=db;Port=5432;Username=$PG_USER;Password=$PG_PASSWORD;Database=tracking_db`
- Users:    `Host=db;Port=5432;Username=$PG_USER;Password=$PG_PASSWORD;Database=users_db`
