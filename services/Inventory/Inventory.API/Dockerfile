FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY shared/BuildingBlocks/BuildingBlocks.csproj shared/BuildingBlocks/
COPY services/Inventory/Inventory.API/Inventory.API.csproj services/Inventory/Inventory.API/
RUN dotnet restore services/Inventory/Inventory.API/Inventory.API.csproj
COPY shared/ shared/
COPY services/Inventory/ services/Inventory/
RUN dotnet publish services/Inventory/Inventory.API/Inventory.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Inventory.API.dll"]
