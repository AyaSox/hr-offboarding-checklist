# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY OffboardingChecklist.csproj ./
RUN dotnet restore OffboardingChecklist.csproj

# Copy all source code
COPY . .

# Build and publish the app project
RUN dotnet publish OffboardingChecklist.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create data directory for SQLite
RUN mkdir -p /app/data

# Copy published output
COPY --from=build /app/publish .

# Environment for Render - bind to PORT
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}

# Expose ports
EXPOSE 8080

# Start
ENTRYPOINT ["dotnet", "OffboardingChecklist.dll"]