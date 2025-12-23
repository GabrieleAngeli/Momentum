FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY Momentum.sln ./
COPY src/services/web-backend-core/WebBackendCore.Domain/ WebBackendCore.Domain/
COPY src/services/web-backend-core/WebBackendCore.Application/ WebBackendCore.Application/
COPY src/services/web-backend-core/WebBackendCore.Infrastructure/ WebBackendCore.Infrastructure/
COPY src/services/web-backend-core/WebBackendCore.Api/ WebBackendCore.Api/

RUN dotnet restore WebBackendCore.Api/WebBackendCore.Api.csproj || true
RUN dotnet publish WebBackendCore.Api/WebBackendCore.Api.csproj -c Release -o /app/publish || true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebBackendCore.Api.dll"]
