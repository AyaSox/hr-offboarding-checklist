# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore first (cache-friendly)
COPY OffboardingChecklist/OffboardingChecklist.csproj ./OffboardingChecklist/
RUN dotnet restore OffboardingChecklist/OffboardingChecklist.csproj

# Copy the rest of the source
COPY OffboardingChecklist/ ./OffboardingChecklist/

# Build and publish
RUN dotnet publish OffboardingChecklist/OffboardingChecklist.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create data directory for SQLite (mounted or ephemeral)
RUN mkdir -p /app/data

# Copy published output
COPY --from=build /app/publish .

# Environment
# Render provides PORT; bind Kestrel to it
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

# Expose default port for local runs (Render ignores EXPOSE)
EXPOSE 8080 80

# Start
ENTRYPOINT ["dotnet", "OffboardingChecklist.dll"]