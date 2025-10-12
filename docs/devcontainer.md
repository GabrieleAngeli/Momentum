# Devcontainer Momentum / Devcontainer di Momentum

## Purpose / Scopo
**English:** The Dev Container provides a reproducible environment for day-to-day development on the Momentum platform.

**Italiano:** Il Dev Container offre un ambiente riproducibile per lo sviluppo quotidiano della piattaforma Momentum.

## Main contents / Contenuti principali
**English:**
- **Custom Dockerfile** based on `mcr.microsoft.com/devcontainers/dotnet` with essential tooling (`make`, `jq`, `git`, `curl`) for builds, tests, and security analysis.
- **Node 20 feature** to support Angular 19 and npm scripts.
- **Docker-in-Docker support** to run `docker compose` from inside the development container.
- **Post-create script** restoring .NET/Angular packages and preparing Playwright for end-to-end tests.
- **VS Code tasks** for `make build`, `make test`, dependency audits (`dotnet list ...`, `npm audit`), and Angular linting.

**Italiano:**
- **Dockerfile personalizzato** basato su `mcr.microsoft.com/devcontainers/dotnet` con tool essenziali (`make`, `jq`, `git`, `curl`) per build, test e analisi di sicurezza.
- **Feature Node 20** per supportare Angular 19 e gli script npm.
- **Supporto Docker-in-Docker** per eseguire `docker compose` dall'interno del container di sviluppo.
- **Script post-create** che ripristina i pacchetti .NET/Angular e prepara Playwright per i test end-to-end.
- **Task VS Code** per `make build`, `make test`, audit dipendenze (`dotnet list ...`, `npm audit`) e linting Angular.

## Usage / Utilizzo
**English:**
1. Install the [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension in VS Code.
2. Open the repository folder and choose `Dev Containers: Reopen in Container`.
3. Wait for the bootstrap script to finish. You will have:
   - .NET and npm dependencies restored;
   - Playwright ready for end-to-end tests (`make e2e`);
   - ports 4200/5000/5001/9090 forwarded to the host for frontend, backend, and observability.
4. Use the available tasks (`Terminal > Run Task...`) to build, test, or run security scans.

**Italiano:**
1. Installa l'estensione [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) in VS Code.
2. Apri la cartella del repository e scegli `Dev Containers: Reopen in Container`.
3. Attendi il completamento dello script di bootstrap. Otterrai:
   - dipendenze .NET e npm ripristinate;
   - Playwright pronto per i test end-to-end (`make e2e`);
   - porte 4200/5000/5001/9090 inoltrate verso l'host per frontend, backend e osservabilità.
4. Utilizza i task disponibili (`Terminal > Run Task...`) per build, test o security scan.

## Helper scripts / Script di supporto
**English:**
- `.devcontainer/scripts/post-create.sh` – initial provisioning (restore packages, set up Playwright).
- `.devcontainer/scripts/post-start.sh` – normalises HTTPS certificate permissions and prints the main commands.
- `.devcontainer/scripts/setup-https-certs.sh` – idempotently creates development HTTPS certificates when missing.

**Italiano:**
- `.devcontainer/scripts/post-create.sh` – provisioning iniziale (restore pacchetti, setup Playwright).
- `.devcontainer/scripts/post-start.sh` – normalizza i permessi dei certificati HTTPS e mostra i comandi principali.
- `.devcontainer/scripts/setup-https-certs.sh` – crea in modo idempotente i certificati HTTPS di sviluppo se mancanti.

## Notes / Note
**English:**
- Development HTTPS certificates are mounted from the host into `/home/vscode/.aspnet/https` to maintain ASP.NET compatibility. On Windows the `%USERPROFILE%\.aspnet\https` folder is also mounted and exposed inside the container.
- To update Playwright or install additional browsers run `npx playwright install --with-deps` inside the container.

**Italiano:**
- I certificati HTTPS di sviluppo sono montati dalla macchina host in `/home/vscode/.aspnet/https` per mantenere la compatibilità con ASP.NET. Su Windows viene montata anche la cartella `%USERPROFILE%\.aspnet\https` e resa disponibile nel container.
- Per aggiornare Playwright o installare browser aggiuntivi esegui `npx playwright install --with-deps` all'interno del container.
