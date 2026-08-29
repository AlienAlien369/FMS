FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ApiGateway/FMS.ApiGateway.csproj", "ApiGateway/"]
COPY ["src/SharedKernel/FMS.SharedKernel.csproj", "SharedKernel/"]
RUN dotnet restore "ApiGateway/FMS.ApiGateway.csproj"
COPY src/ApiGateway/ ApiGateway/
COPY src/SharedKernel/ SharedKernel/
RUN dotnet build "ApiGateway/FMS.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiGateway/FMS.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FMS.ApiGateway.dll"]
