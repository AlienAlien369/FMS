FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["src/API/FMS.API.csproj", "src/API/"]
COPY ["src/Application/FMS.Application.csproj", "src/Application/"]
COPY ["src/Domain/FMS.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/FMS.Infrastructure.csproj", "src/Infrastructure/"]
RUN dotnet restore "src/API/FMS.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/API"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "FMS.API.dll"]
