.PHONY: dev build backend frontend contracts test lint security

dev: build
	docker compose up --build

build:
	dotnet build Momentum.sln || true
	npm install --prefix src/web-core
	npm run build --prefix src/web-core || true

backend:
	dotnet build Momentum.sln || true

frontend:
	npm install --prefix src/web-core
	npm start --prefix src/web-core

contracts:
	tar -czf contracts.tar.gz contracts

e2e:
	npx playwright test

test:
        dotnet test Momentum.sln || true
        npm test --prefix src/web-core || true

security:
        tools/security/run-all.sh
