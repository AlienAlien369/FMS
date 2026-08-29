# ============================================
# FMS API — Combined Microservices Dockerfile
# Builds the full solution and runs the legacy API
# (which has all controllers for auth, vehicles, drivers, devices, config)
# ============================================

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY FMS.sln .

# Copy all project files for restore caching
COPY src/Domain/FMS.Domain.csproj src/Domain/
COPY src/Application/FMS.Application.csproj src/Application/
COPY src/Infrastructure/FMS.Infrastructure.csproj src/Infrastructure/
COPY src/API/FMS.API.csproj src/API/
COPY src/SharedKernel/FMS.SharedKernel.csproj src/SharedKernel/
COPY src/MessageBus/FMS.MessageBus.csproj src/MessageBus/

# Restore (cached layer)
RUN dotnet restore FMS.sln

# Copy all source code
COPY src/ src/

# Build and publish the API project (which references everything)
WORKDIR /src/src/API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "FMS.API.dll"]
