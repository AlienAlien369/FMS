FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Notification/FMS.Notification.csproj", "Services/Notification/"]
COPY ["src/SharedKernel/FMS.SharedKernel.csproj", "SharedKernel/"]
COPY ["src/MessageBus/FMS.MessageBus.csproj", "MessageBus/"]
RUN dotnet restore "Services/Notification/FMS.Notification.csproj"
COPY src/Services/Notification/ Services/Notification/
COPY src/SharedKernel/ SharedKernel/
COPY src/MessageBus/ MessageBus/
RUN dotnet build "Services/Notification/FMS.Notification.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Notification/FMS.Notification.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FMS.Notification.dll"]
