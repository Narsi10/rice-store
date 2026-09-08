FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY shared/BuildingBlocks/BuildingBlocks.csproj shared/BuildingBlocks/
COPY services/Notification/Notification.API/Notification.API.csproj services/Notification/Notification.API/
RUN dotnet restore services/Notification/Notification.API/Notification.API.csproj
COPY shared/ shared/
COPY services/Notification/ services/Notification/
RUN dotnet publish services/Notification/Notification.API/Notification.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Notification.API.dll"]
