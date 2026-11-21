FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY OWSInventoryService/OWSInventoryService.csproj OWSInventoryService/
RUN dotnet restore OWSInventoryService/OWSInventoryService.csproj
COPY OWSInventoryService/ OWSInventoryService/
RUN dotnet publish OWSInventoryService/OWSInventoryService.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OWSInventoryService.dll"]
