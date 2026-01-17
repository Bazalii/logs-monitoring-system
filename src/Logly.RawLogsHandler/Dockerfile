FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

COPY ["src/Logly.RawLogsHandler/Logly.RawLogsHandler.csproj", "src/Logly.RawLogsHandler/"]
COPY ["src/Logly.Kafka/Logly.Kafka.csproj", "src/Logly.Kafka/"]
COPY ["src/Logly.Serialization/Logly.Serialization.csproj", "src/Logly.Serialization/"]
RUN dotnet restore "src/Logly.RawLogsHandler/Logly.RawLogsHandler.csproj"
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
