# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all source code
COPY . .

# Build and publish the app
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create data directory for SQLite with proper permissions
RUN mkdir -p /app/data && chmod 755 /app/data

# Copy published output
COPY --from=build /app/publish .

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}
ENV DATABASE_PROVIDER=Sqlite
ENV SQLITE_DB_PATH=/app/data/offboarding.db
ENV RECREATE_DATABASE=true

# Expose ports
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "OffboardingChecklist.dll"]