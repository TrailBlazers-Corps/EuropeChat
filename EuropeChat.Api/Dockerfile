FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EuropeChat.Api/EuropeChat.Api.csproj", "EuropeChat.Api/"]
COPY ["EuropeChat.Api.IntegrationTests/EuropeChat.Api.IntegrationTests.csproj", "EuropeChat.Api.IntegrationTests/"]
RUN dotnet restore "EuropeChat.Api/EuropeChat.Api.csproj"
COPY . .
WORKDIR "/src/EuropeChat.Api"
RUN dotnet build "EuropeChat.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EuropeChat.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY EuropeChat.Api/appsettings.json .
ENTRYPOINT ["dotnet", "EuropeChat.Api.dll"]
