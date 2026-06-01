#!/usr/bin/env bash
# One command to bring up the whole local stack (backbone §13.2):
# infra (compose) → API host → generated client → Notice PWA.
set -euo pipefail
cd "$(dirname "$0")/.."

API_URL="http://localhost:5080"

echo "▶ starting local infra (postgres, minio, mailpit, proxy)…"
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

echo "▶ starting the Notice PWA at http://localhost:5173 …"
cd web/notice && npm run dev
