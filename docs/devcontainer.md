# Devcontainer Momentum

La configurazione del Dev Container permette di avere un ambiente riproducibile per lo sviluppo quotidiano della piattaforma Momentum.

## Contenuto principale

- **Dockerfile personalizzato** basato su `mcr.microsoft.com/devcontainers/dotnet` con tool di base (`make`, `jq`, `git`, `curl`) utili per build, test e analisi sicurezza.
- **Feature Node 20** per supportare il frontend Angular 19 e gli script npm.
- **Supporto Docker-in-Docker** per eseguire `docker compose` direttamente dal container di sviluppo.
- **Script post-create** che ripristina i pacchetti .NET/Angular e prepara Playwright per gli end-to-end test.
- **Task VS Code** preconfigurati per `make build`, `make test`, audit dipendenze (`dotnet list ...` e `npm audit`) e linting Angular.

## Utilizzo

1. Installa l'estensione [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) in VS Code.
2. Apri la cartella del repository e scegli `Dev Containers: Reopen in Container`.
3. Attendi il completamento dello script di bootstrap. Al termine avrai:
   - dipendenze .NET e npm già ripristinate;
   - Playwright pronto per gli end-to-end test (`make e2e`);
   - porte 4200/5000/5001/9090 inoltrate verso l'host per frontend, backend e osservabilità.
4. Utilizza i task disponibili (`Terminal > Run Task...`) per build, test o security scan.

## Script disponibili

- `.devcontainer/scripts/post-create.sh` – provisioning iniziale (restore pacchetti, setup Playwright).
- `.devcontainer/scripts/post-start.sh` – normalizza permessi certificati HTTPS e mostra i comandi principali.

## Note

- I certificati di sviluppo HTTPS sono montati dalla macchina host in `/home/vscode/.aspnet/https` per mantenere la compatibilità con ASP.NET.
- Per aggiornare Playwright o installare browser aggiuntivi eseguire `npx playwright install --with-deps` all'interno del container.
