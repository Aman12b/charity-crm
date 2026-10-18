#!/usr/bin/env bash
# Backup of the database and the app volume (invoice PDFs + data protection keys) – NFR-REL-04.
#
#   scripts/backup.sh [backup-dir]            default: ./backups
#   COMPOSE_PROJECT_NAME=other scripts/backup.sh
#
# Creates db-<timestamp>.dump (pg_dump custom format) and appdata-<timestamp>.tar.gz.
set -euo pipefail
cd "$(dirname "$0")/.."
export MSYS_NO_PATHCONV=1 # Git Bash on Windows: keep docker volume paths untouched

PROJECT="${COMPOSE_PROJECT_NAME:-charity-crm}"
DIR="${1:-./backups}"
STAMP="$(date +%Y%m%d-%H%M%S)"
mkdir -p "$DIR"
ABS_DIR="$(cd "$DIR" && (pwd -W 2>/dev/null || pwd))"

echo "Backing up database of project '$PROJECT'..."
docker compose -p "$PROJECT" exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc' > "$DIR/db-$STAMP.dump"

echo "Backing up volume ${PROJECT}_appdata..."
docker run --rm -v "${PROJECT}_appdata:/data:ro" -v "$ABS_DIR:/backup" alpine:3 \
  tar czf "/backup/appdata-$STAMP.tar.gz" -C /data .

echo "Done:"
ls -l "$DIR/db-$STAMP.dump" "$DIR/appdata-$STAMP.tar.gz"
