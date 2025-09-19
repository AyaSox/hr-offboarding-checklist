# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY OffboardingChecklist/OffboardingChecklist.csproj ./OffboardingChecklist/
RUN dotnet restore OffboardingChecklist/OffboardingChecklist.csproj

# Copy all source files (ensure Data folder is included)
COPY OffboardingChecklist/ ./OffboardingChecklist/

# Verify Data folder exists
RUN ls -la ./OffboardingChecklist/Data/ || echo "Data folder missing!"

# Build and publish
RUN dotnet publish OffboardingChecklist/OffboardingChecklist.csproj -c Release -o /app/publish --no-restore

# Use the official .NET 8 runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create data directory for SQLite (mounted or ephemeral)
RUN mkdir -p /app/data

# Copy the published application
COPY --from=build /app/publish .

# Environment
# Render provides PORT; bind Kestrel to it
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}

# Expose default port for local runs (Render ignores EXPOSE)
EXPOSE 8080

# Drop privileges (good practice)
USER $APP_UID

# Set the entry point
ENTRYPOINT ["dotnet", "OffboardingChecklist.dll"]