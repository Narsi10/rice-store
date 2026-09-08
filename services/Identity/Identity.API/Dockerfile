FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY shared/BuildingBlocks/BuildingBlocks.csproj shared/BuildingBlocks/
COPY services/Identity/Identity.API/Identity.API.csproj services/Identity/Identity.API/
RUN dotnet restore services/Identity/Identity.API/Identity.API.csproj
COPY shared/ shared/
COPY services/Identity/ services/Identity/
RUN dotnet publish services/Identity/Identity.API/Identity.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Identity.API.dll"]
