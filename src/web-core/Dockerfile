FROM node:22-bookworm-slim AS build
WORKDIR /repo

# Copio anche il package "momentum-release-automation" dalla root
COPY package.json ./package.json

# Copio web-core manifests
COPY src/web-core/package.json src/web-core/package-lock.json ./src/web-core/

# Install dentro la cartella web-core, ma con root presente così file:../.. risolve
WORKDIR /repo/src/web-core
RUN npm ci --no-audit --no-fund

COPY src/web-core/ ./
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /repo/src/web-core/dist/web-core /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
