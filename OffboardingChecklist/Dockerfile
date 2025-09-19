# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["OffboardingChecklist.csproj", "."]
RUN dotnet restore "OffboardingChecklist.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "OffboardingChecklist.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "OffboardingChecklist.csproj" -c Release -o /app/publish

# Use the official .NET 8 runtime image for the final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Expose the port the app runs on
EXPOSE 80
EXPOSE 443

# Set the entry point
ENTRYPOINT ["dotnet", "OffboardingChecklist.dll"]