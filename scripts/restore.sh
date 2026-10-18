#!/usr/bin/env bash
# Restores a backup created by scripts/backup.sh – NFR-REL-04.
#
#   scripts/restore.sh <db-dump> <appdata-tar.gz> [--yes]
#
# The API is stopped during the restore. Existing data is REPLACED.
set -euo pipefail
cd "$(dirname "$0")/.."
export MSYS_NO_PATHCONV=1

if [ $# -lt 2 ]; then
  echo "Usage: $0 <db-dump> <appdata-tar.gz> [--yes]" >&2
  exit 1
fi
DUMP="$1"
ARCHIVE="$2"
PROJECT="${COMPOSE_PROJECT_NAME:-charity-crm}"
[ -f "$DUMP" ] || { echo "Not found: $DUMP" >&2; exit 1; }
[ -f "$ARCHIVE" ] || { echo "Not found: $ARCHIVE" >&2; exit 1; }

if [ "${3:-}" != "--yes" ]; then
  read -r -p "All current data of project '$PROJECT' will be replaced. Continue? [y/N] " answer
  [ "$answer" = "y" ] || [ "$answer" = "Y" ] || { echo "Aborted."; exit 1; }
fi

ARCHIVE_DIR="$(cd "$(dirname "$ARCHIVE")" && (pwd -W 2>/dev/null || pwd))"
ARCHIVE_NAME="$(basename "$ARCHIVE")"

echo "Stopping API..."
docker compose -p "$PROJECT" stop api

echo "Restoring database..."
docker compose -p "$PROJECT" exec -T db sh -c \
  'pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists --no-owner --single-transaction' < "$DUMP"

echo "Restoring volume ${PROJECT}_appdata..."
docker run --rm -v "${PROJECT}_appdata:/data" -v "$ARCHIVE_DIR:/backup:ro" alpine:3 \
  sh -c "rm -rf /data/* /data/.[!.]* 2>/dev/null; tar xzf '/backup/$ARCHIVE_NAME' -C /data"

echo "Starting API..."
docker compose -p "$PROJECT" start api
echo "Restore complete."
