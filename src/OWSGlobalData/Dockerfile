#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OWSGlobalData/OWSGlobalData.csproj", "OWSGlobalData/"]
COPY ["OWSShared/OWSShared.csproj", "OWSShared/"]
COPY ["OWSData/OWSData.csproj", "OWSData/"]
RUN dotnet restore "OWSGlobalData/OWSGlobalData.csproj"
COPY . .
WORKDIR "/src/OWSGlobalData"
RUN dotnet build "OWSGlobalData.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OWSGlobalData.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OWSGlobalData.dll"]