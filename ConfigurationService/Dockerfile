# Этап 1: Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копирование локального NuGet пакета
COPY ["local-nuget/", "/local-nuget/"]

# Копирование .csproj файлов и восстановление зависимостей
COPY ["ConfigurationService/ConfigurationService.csproj", "ConfigurationService/"]
RUN dotnet restore "ConfigurationService/ConfigurationService.csproj" --source /local-nuget --source https://api.nuget.org/v3/index.json

# Копирование остального кода и сборка
COPY . .
WORKDIR "/src/ConfigurationService"
RUN dotnet build "ConfigurationService.csproj" -c Release -o /app/build

# Этап 2: Публикация приложения
FROM build AS publish
RUN dotnet publish "ConfigurationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Этап 3: Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Установка curl для healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Создание директории для логов
RUN mkdir -p /app/logs && chmod 777 /app/logs

# Копирование опубликованного приложения
COPY --from=publish /app/publish .

# Настройка переменных окружения
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Открытие порта
EXPOSE 80

# Healthcheck
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

# Запуск приложения
ENTRYPOINT ["dotnet", "Configuration.Service.dll"]
