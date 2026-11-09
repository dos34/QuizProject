# Сборка из корня репозитория: docker build -f src/Quiz.Realtime/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только файлы проектов — слой restore кешируется, пока не меняются зависимости.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Quiz.Contracts/Quiz.Contracts.csproj src/Quiz.Contracts/
COPY src/Quiz.Domain/Quiz.Domain.csproj src/Quiz.Domain/
COPY src/Quiz.Infrastructure/Quiz.Infrastructure.csproj src/Quiz.Infrastructure/
COPY src/Quiz.Realtime/Quiz.Realtime.csproj src/Quiz.Realtime/
RUN dotnet restore src/Quiz.Realtime/Quiz.Realtime.csproj

COPY src/ src/
RUN dotnet publish src/Quiz.Realtime/Quiz.Realtime.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=80 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 80
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Quiz.Realtime.dll"]
