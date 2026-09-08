FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY shared/BuildingBlocks/BuildingBlocks.csproj shared/BuildingBlocks/
COPY services/Order/Order.API/Order.API.csproj services/Order/Order.API/
RUN dotnet restore services/Order/Order.API/Order.API.csproj
COPY shared/ shared/
COPY services/Order/ services/Order/
RUN dotnet publish services/Order/Order.API/Order.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Order.API.dll"]
