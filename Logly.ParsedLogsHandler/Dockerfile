FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Logly.ParsedLogsHandler/Logly.ParsedLogsHandler.csproj", "Logly.ParsedLogsHandler/"]
COPY ["Logly.Kafka/Logly.Kafka.csproj", "Logly.Kafka/"]
COPY ["Logly.Serialization/Logly.Serialization.csproj", "Logly.Serialization/"]
RUN dotnet restore "Logly.ParsedLogsHandler/Logly.ParsedLogsHandler.csproj"
COPY . .
WORKDIR "/src/Logly.ParsedLogsHandler"
RUN dotnet build "./Logly.ParsedLogsHandler.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Logly.ParsedLogsHandler.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Logly.ParsedLogsHandler.dll"]
