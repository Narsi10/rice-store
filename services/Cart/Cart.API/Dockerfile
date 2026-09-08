FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY shared/BuildingBlocks/BuildingBlocks.csproj shared/BuildingBlocks/
COPY services/Cart/Cart.API/Cart.API.csproj services/Cart/Cart.API/
RUN dotnet restore services/Cart/Cart.API/Cart.API.csproj
COPY shared/ shared/
COPY services/Cart/ services/Cart/
RUN dotnet publish services/Cart/Cart.API/Cart.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Cart.API.dll"]
