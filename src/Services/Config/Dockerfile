FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Config/FMS.Config.csproj", "Services/Config/"]
COPY ["src/SharedKernel/FMS.SharedKernel.csproj", "SharedKernel/"]
COPY ["src/Domain/FMS.Domain.csproj", "Domain/"]
COPY ["src/MessageBus/FMS.MessageBus.csproj", "MessageBus/"]
RUN dotnet restore "Services/Config/FMS.Config.csproj"
COPY src/Services/Config/ Services/Config/
COPY src/SharedKernel/ SharedKernel/
COPY src/Domain/ Domain/
COPY src/MessageBus/ MessageBus/
RUN dotnet build "Services/Config/FMS.Config.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Config/FMS.Config.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FMS.Config.dll"]
