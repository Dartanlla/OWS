#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 44303

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["OWSPublicAPI/OWSPublicAPI.csproj", "OWSPublicAPI/"]
COPY ["OWSExternalLoginProviders/OWSExternalLoginProviders.csproj", "OWSExternalLoginProviders/"]
COPY ["OWSShared/OWSShared.csproj", "OWSShared/"]
COPY ["OWSData/OWSData.csproj", "OWSData/"]
RUN dotnet restore "OWSPublicAPI/OWSPublicAPI.csproj"
COPY . .
WORKDIR "/src/OWSPublicAPI"
RUN dotnet build "OWSPublicAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OWSPublicAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OWSPublicAPI.dll"]