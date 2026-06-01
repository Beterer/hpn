.DEFAULT_GOAL := help
.PHONY: help up down dev api web test build gen-api fmt

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

test: ## Run all backend tests (unit + architecture + integration)
	dotnet test HPN.sln

build: ## Release build of backend + frontend
	dotnet build HPN.sln -c Release
	cd web/notice && npm ci && npm run build
