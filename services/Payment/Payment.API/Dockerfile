FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY shared/BuildingBlocks/BuildingBlocks.csproj shared/BuildingBlocks/
COPY services/Payment/Payment.API/Payment.API.csproj services/Payment/Payment.API/
RUN dotnet restore services/Payment/Payment.API/Payment.API.csproj
COPY shared/ shared/
COPY services/Payment/ services/Payment/
RUN dotnet publish services/Payment/Payment.API/Payment.API.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Payment.API.dll"]
