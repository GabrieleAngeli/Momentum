# Devcontainer Momentum

## Purpose
The Dev Container provides a reproducible environment for day-to-day development on the Momentum platform.

## Main contents
- **Custom Dockerfile** based on `mcr.microsoft.com/devcontainers/dotnet` with essential tooling (`make`, `jq`, `git`, `curl`) for builds, tests, and security analysis.
- **Node 20 feature** to support Angular 19 and npm scripts.
- **Docker-in-Docker support** to run `docker compose` from inside the development container.
- **Post-create script** restoring .NET/Angular packages and preparing Playwright for end-to-end tests.
- **VS Code tasks** for `make build`, `make test`, dependency audits (`dotnet list ...`, `npm audit`), and Angular linting.

## Usage
1. Install the [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension in VS Code.
2. Open the repository folder and choose `Dev Containers: Reopen in Container`.
3. Wait for the bootstrap script to finish. You will have:
   - .NET and npm dependencies restored;
   - Playwright ready for end-to-end tests (`make e2e`);
   - ports 4200/5000/5001/9090 forwarded to the host for frontend, backend, and observability.
4. Use the available tasks (`Terminal > Run Task...`) to build, test, or run security scans.
5. For modular monolith development, the container exposes all prerequisites (Dapr CLI, Aspire tooling, Node.js) required by the [Modular Architecture Guidelines](06-Modular-Architecture-Guidelines.md).

## Helper scripts
- `.devcontainer/scripts/post-create.sh` – initial provisioning (restore packages, set up Playwright).
- `.devcontainer/scripts/post-start.sh` – normalises HTTPS certificate permissions and prints the main commands.
- `.devcontainer/scripts/setup-https-certs.sh` – idempotently creates development HTTPS certificates when missing.

## Notes
- Development HTTPS certificates are mounted from the host into `/home/vscode/.aspnet/https` to maintain ASP.NET compatibility. On Windows the `%USERPROFILE%\.aspnet\https` folder is also mounted and exposed inside the container.
- To update Playwright or install additional browsers run `npx playwright install --with-deps` inside the container.
