.DEFAULT_GOAL := help
.PHONY: help up down dev api web test build gen-api geoip fmt

# Offline IP→country database for the same-country privacy filter (ADR-028).
# DB-IP Lite Country (CC BY 4.0), mirrored as mmdb by sapics/ip-location-db.
# Override GEOIP_URL to use a different source (e.g. a MaxMind GeoLite2-Country.mmdb).
GEOIP_URL ?= https://raw.githubusercontent.com/sapics/ip-location-db/main/dbip-country-mmdb/dbip-country.mmdb
GEOIP_PATH := src/Hpn.Api/App_Data/dbip-country.mmdb

help: ## List targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-12s\033[0m %s\n", $$1, $$2}'

up: ## Start local infra (postgres:55432, minio:19000/19001, mailpit:18025, proxy:18080)
	docker compose up -d

down: ## Stop local infra
	docker compose down

dev: ## Bring up everything: infra + API + web (foreground)
	./scripts/dev.sh

api: ## Run the API host only
	dotnet run --project src/Hpn.Api

web: ## Run the Notice PWA dev server only
	cd web/notice && npm run dev

gen-api: ## Regenerate the typed API client from the OpenAPI document
	dotnet build src/Hpn.Api/Hpn.Api.csproj -c Debug
	cd web/notice && npm run generate:api

geoip: ## Fetch the offline IP→country database (DB-IP Lite, CC BY 4.0) into App_Data
	@mkdir -p $(dir $(GEOIP_PATH))
	curl -fL --retry 3 -o $(GEOIP_PATH) "$(GEOIP_URL)"
	@echo "Saved $(GEOIP_PATH) — set GeoIp:DatabasePath to App_Data/$(notdir $(GEOIP_PATH)) (default)."

test: ## Run all backend tests (unit + architecture + integration)
	dotnet test HPN.sln

build: ## Release build of backend + frontend
	dotnet build HPN.sln -c Release
	cd web/notice && npm ci && npm run build
