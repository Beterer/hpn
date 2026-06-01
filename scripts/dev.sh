#!/usr/bin/env bash
# One command to bring up the whole local stack (backbone §13.2):
# infra (compose) → API host → generated client → Notice PWA.
set -euo pipefail
cd "$(dirname "$0")/.."

API_URL="http://localhost:5080"
WEB_URL="http://localhost:5173"
MAILPIT_URL="http://localhost:18025"

echo "▶ starting local infra (postgres:55432, minio:19000/19001, mailpit:${MAILPIT_URL}, proxy:18080)…"
docker compose up -d

echo "▶ starting API host at ${API_URL} …"
dotnet run --project src/Hpn.Api &
API_PID=$!
cleanup() { kill "${API_PID}" 2>/dev/null || true; }
trap cleanup EXIT

echo "▶ waiting for the OpenAPI document…"
until curl -sf "${API_URL}/openapi/v1.json" >/dev/null 2>&1; do
  sleep 1
done

echo "▶ generating the typed API client…"
( cd web/notice && npm run generate:api )

echo "▶ Mailpit inbox: ${MAILPIT_URL}"
echo "▶ starting the Notice PWA at ${WEB_URL} …"
cd web/notice && npm run dev
