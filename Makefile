.PHONY: dev build backend frontend contracts test lint security

dev: build
	docker compose up --build

build:
	dotnet build Momentum.sln || true
	npm ci --prefix src/web-core
	npm run build --prefix src/web-core || true

backend:
	dotnet build Momentum.sln || true

frontend:
	npm ci --prefix src/web-core
	npm start --prefix src/web-core

contracts:
	tar -czf contracts.tar.gz contracts

e2e:
	npx playwright test

test:
	-dotnet test Momentum.sln
	-npm test --prefix src/web-core -- --watch=false --browsers=ChromeHeadless

security:
	tools/security/run-all.sh
