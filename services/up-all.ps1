# up-all.ps1
[CmdletBinding()]
param()

$files = @(
  "services/HabitsService/docker-compose.yml",
  "services/TrackingService/docker-compose.yml",
  "services/UserService/docker-compose.yml"
)

$args = $files | ForEach-Object { "-f"; $_ }

docker compose @args up --build --force-recreate -d