FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Entity/FMS.Entity.csproj", "Services/Entity/"]
COPY ["src/SharedKernel/FMS.SharedKernel.csproj", "SharedKernel/"]
COPY ["src/Domain/FMS.Domain.csproj", "Domain/"]
COPY ["src/MessageBus/FMS.MessageBus.csproj", "MessageBus/"]
RUN dotnet restore "Services/Entity/FMS.Entity.csproj"
COPY src/Services/Entity/ Services/Entity/
COPY src/SharedKernel/ SharedKernel/
COPY src/Domain/ Domain/
COPY src/MessageBus/ MessageBus/
RUN dotnet build "Services/Entity/FMS.Entity.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Entity/FMS.Entity.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FMS.Entity.dll"]
