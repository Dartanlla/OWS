#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OWSManagement/OWSManagement.csproj", "OWSManagement/"]
COPY ["OWSShared/OWSShared.csproj", "OWSShared/"]
COPY ["OWSData/OWSData.csproj", "OWSData/"]
RUN dotnet restore "OWSManagement/OWSManagement.csproj"
COPY . .
WORKDIR "/src/OWSManagement"
RUN dotnet build "OWSManagement.csproj" -c Release -o /app/build

FROM node:latest as build-node
WORKDIR /wwwroot
COPY --from=build /src/OWSManagement/wwwroot .
RUN npm install
RUN npm run build

FROM build AS publish
RUN dotnet publish "OWSManagement.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build-node /wwwroot/dist ./wwwroot/dist
ENTRYPOINT ["dotnet", "OWSManagement.dll"]