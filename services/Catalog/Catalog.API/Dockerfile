FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY shared/BuildingBlocks/BuildingBlocks.csproj shared/BuildingBlocks/
COPY services/Catalog/Catalog.API/Catalog.API.csproj services/Catalog/Catalog.API/
RUN dotnet restore services/Catalog/Catalog.API/Catalog.API.csproj
COPY shared/ shared/
COPY services/Catalog/ services/Catalog/
RUN dotnet publish services/Catalog/Catalog.API/Catalog.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
