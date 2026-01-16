FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Logly.LogsGenerator/Logly.LogsGenerator.csproj", "Logly.LogsGenerator/"]
COPY ["Logly.Kafka/Logly.Kafka.csproj", "Logly.Kafka/"]
COPY ["Logly.Serialization/Logly.Serialization.csproj", "Logly.Serialization/"]
RUN dotnet restore "Logly.LogsGenerator/Logly.LogsGenerator.csproj"
COPY . .
WORKDIR "/src/Logly.LogsGenerator"
RUN dotnet build "./Logly.LogsGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Logly.LogsGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Logly.LogsGenerator.dll"]
