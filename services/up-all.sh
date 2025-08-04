#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

docker network create habitstracker-net

for svc in pg_db HabitsService TrackingService UserService; do
  echo "=== Starting $svc container ==="
  (cd "$svc" && docker compose up --build --force-recreate -d)
done

echo "Done."