FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/DataCollector/FMS.DataCollector.csproj", "Services/DataCollector/"]
COPY ["src/SharedKernel/FMS.SharedKernel.csproj", "SharedKernel/"]
COPY ["src/MessageBus/FMS.MessageBus.csproj", "MessageBus/"]
RUN dotnet restore "Services/DataCollector/FMS.DataCollector.csproj"
COPY src/Services/DataCollector/ Services/DataCollector/
COPY src/SharedKernel/ SharedKernel/
COPY src/MessageBus/ MessageBus/
RUN dotnet build "Services/DataCollector/FMS.DataCollector.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/DataCollector/FMS.DataCollector.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FMS.DataCollector.dll"]
