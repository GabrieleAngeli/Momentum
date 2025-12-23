FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./

COPY Momentum.sln ./
COPY src/services/notifier/Notifier.Domain/Notifier.Domain.csproj src/services/notifier/Notifier.Domain/
COPY src/services/notifier/Notifier.Application/Notifier.Application.csproj src/services/notifier/Notifier.Application/
COPY src/services/notifier/Notifier.Infrastructure/Notifier.Infrastructure.csproj src/services/notifier/Notifier.Infrastructure/
COPY src/services/notifier/Notifier.Api/Notifier.Api.csproj src/services/notifier/Notifier.Api/
RUN dotnet restore src/services/notifier/Notifier.Api/Notifier.Api.csproj

COPY . .
RUN dotnet publish src/services/notifier/Notifier.Api/Notifier.Api.csproj -c Release -o /app/out /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Notifier.Api.dll"]
