# Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for restore (main + migration projects)
COPY ["pi-planning-backend/pi-planning-backend.csproj", "pi-planning-backend/"]
COPY ["pi-planning-backend.migrations.postgres/pi-planning-backend.migrations.postgres.csproj", "pi-planning-backend.migrations.postgres/"]
COPY ["pi-planning-backend.migrations.sqlserver/pi-planning-backend.migrations.sqlserver.csproj", "pi-planning-backend.migrations.sqlserver/"]

# Restore dependencies
RUN dotnet restore "pi-planning-backend/pi-planning-backend.csproj"

# Copy all source code
COPY . .

# Build
WORKDIR "/src/pi-planning-backend"
RUN dotnet build "pi-planning-backend.csproj" -c Release -o /app/build

# Build migration projects (so their DLLs exist for the publish step)
WORKDIR "/src/pi-planning-backend.migrations.postgres"
RUN dotnet build "pi-planning-backend.migrations.postgres.csproj" -c Release -o /app/build

WORKDIR "/src/pi-planning-backend.migrations.sqlserver"
RUN dotnet build "pi-planning-backend.migrations.sqlserver.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
WORKDIR "/src/pi-planning-backend"
# The publish target automatically includes migration DLLs via IncludeMigrationAssembliesInPublish
RUN dotnet publish "pi-planning-backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "pi-planning-backend.dll"]
