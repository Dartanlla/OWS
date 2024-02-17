#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OWSManagement/OWSManagement.csproj", "OWSManagement/"]
COPY ["OWSData/OWSData.csproj", "OWSData/"]
COPY ["OWSShared/OWSShared.csproj", "OWSShared/"]
RUN dotnet restore "./OWSManagement/./OWSManagement.csproj"
COPY . .
WORKDIR "/src/OWSManagement"
RUN dotnet build "./OWSManagement.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OWSManagement.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OWSManagement.dll"]