FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
COPY ["src/Logly.LogsAnalyzer/Logly.LogsAnalyzer.csproj", "src/Logly.LogsAnalyzer/"]
COPY ["src/Logly.Serialization/Logly.Serialization.csproj", "src/Logly.Serialization/"]
RUN dotnet restore "src/Logly.LogsAnalyzer/Logly.LogsAnalyzer.csproj"
COPY . .
WORKDIR "/src/Logly.LogsAnalyzer"
RUN dotnet build "./Logly.LogsAnalyzer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Logly.LogsAnalyzer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Logly.LogsAnalyzer.dll"]
