#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OWSContent/OWSContent.csproj", "OWSContent/"]
COPY ["OWSShared/OWSShared.csproj", "OWSShared/"]
RUN dotnet restore "OWSContent/OWSContent.csproj"
COPY . .
WORKDIR "/src/OWSContent"
RUN dotnet build "OWSContent.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OWSContent.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OWSContent.dll"]