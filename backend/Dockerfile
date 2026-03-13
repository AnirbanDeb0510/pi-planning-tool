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

# Restore dependencies (main project + migration projects — all three must be restored
# before publish because the CopyMigrationAssemblies AfterBuild target MSBuilds them)
RUN dotnet restore "pi-planning-backend/pi-planning-backend.csproj" \
    && dotnet restore "pi-planning-backend.migrations.postgres/pi-planning-backend.migrations.postgres.csproj" \
    && dotnet restore "pi-planning-backend.migrations.sqlserver/pi-planning-backend.migrations.sqlserver.csproj"

# Copy all source code
COPY . .

# Publish — dotnet publish implicitly builds first; the CopyMigrationAssemblies MSBuild target
# in pi-planning-backend.csproj builds the migration projects automatically during the build phase.
WORKDIR "/src/pi-planning-backend"
RUN dotnet publish "pi-planning-backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "pi-planning-backend.dll"]
