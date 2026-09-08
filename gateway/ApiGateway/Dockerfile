FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY gateway/ApiGateway/ApiGateway.csproj gateway/ApiGateway/
RUN dotnet restore gateway/ApiGateway/ApiGateway.csproj
COPY gateway/ gateway/
RUN dotnet publish gateway/ApiGateway/ApiGateway.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
