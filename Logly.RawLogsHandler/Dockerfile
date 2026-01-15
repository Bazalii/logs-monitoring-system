FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Logly.RawLogsHandler/Logly.RawLogsHandler.csproj", "Logly.RawLogsHandler/"]
RUN dotnet restore "Logly.RawLogsHandler/Logly.RawLogsHandler.csproj"
COPY . .
WORKDIR "/src/Logly.RawLogsHandler"
RUN dotnet build "./Logly.RawLogsHandler.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Logly.RawLogsHandler.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Logly.RawLogsHandler.dll"]
