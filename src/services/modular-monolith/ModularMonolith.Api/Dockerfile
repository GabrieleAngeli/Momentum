FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY Momentum.sln ./
COPY src/services/modular-monolith/ModularMonolith.Domain/ModularMonolith.Domain.csproj src/services/modular-monolith/ModularMonolith.Domain/
COPY src/services/modular-monolith/ModularMonolith.Application/ModularMonolith.Application.csproj src/services/modular-monolith/ModularMonolith.Application/
COPY src/services/modular-monolith/ModularMonolith.Infrastructure/ModularMonolith.Infrastructure.csproj src/services/modular-monolith/ModularMonolith.Infrastructure/
COPY src/services/modular-monolith/ModularMonolith.Api/ModularMonolith.Api.csproj src/services/modular-monolith/ModularMonolith.Api/
RUN dotnet restore src/services/modular-monolith/ModularMonolith.Api/ModularMonolith.Api.csproj

COPY . .
RUN dotnet publish src/services/modular-monolith/ModularMonolith.Api/ModularMonolith.Api.csproj -c Release -o /app/out /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "ModularMonolith.Api.dll"]
