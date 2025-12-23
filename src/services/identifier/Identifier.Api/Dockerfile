FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src


COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY Momentum.sln ./
COPY src/services/identifier/Identifier.Domain/Identifier.Domain.csproj src/services/identifier/Identifier.Domain/
COPY src/services/identifier/Identifier.Application/Identifier.Application.csproj src/services/identifier/Identifier.Application/
COPY src/services/identifier/Identifier.Infrastructure/Identifier.Infrastructure.csproj src/services/identifier/Identifier.Infrastructure/
COPY src/services/identifier/Identifier.Api/Identifier.Api.csproj src/services/identifier/Identifier.Api/
RUN dotnet restore src/services/identifier/Identifier.Api/Identifier.Api.csproj

COPY . .
RUN dotnet publish src/services/identifier/Identifier.Api/Identifier.Api.csproj -c Release -o /app/out /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Identifier.Api.dll"]
