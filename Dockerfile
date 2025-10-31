# Build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# copy csproj and restore first (layer caching)
COPY ["TempletonTestApi/TempletonTestApi.csproj", "TempletonTestApi/"]
RUN dotnet restore "TempletonTestApi/TempletonTestApi.csproj"

# copy the rest and build
COPY . .
WORKDIR "/src/TempletonTestApi"
RUN dotnet build "TempletonTestApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TempletonTestApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Kestrel in .NET containers defaults to http://+:8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TempletonTestApi.dll"]