# Встановлюємо потрібну .NET версію
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Копіюємо csproj та відновлюємо пакети
COPY *.csproj ./
RUN dotnet restore

# Копіюємо решту коду та збираємо
COPY . ./
RUN dotnet publish -c Release -o out

# Запуск runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out ./

# Порт, який Render надає через $PORT
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_HOST_PATH=/usr/bin/dotnet
EXPOSE 8080
ENTRYPOINT ["dotnet", "myFridge.dll"]
