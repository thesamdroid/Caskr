# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM dotnet/aspnet:8.0-jammy AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM dotnet/sdk:8.0-jammy AS with-node
RUN apt-get update \
    && apt-get install -y curl \
    && curl -sL https://deb.nodesource.com/setup_20.x | bash \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*


FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Caskr.Server/Caskr.Server.csproj", "Caskr.Server/"]
COPY ["caskr.client/caskr.client.esproj", "caskr.client/"]
RUN dotnet restore "./Caskr.Server/Caskr.Server.csproj"
COPY . .
WORKDIR "/src/Caskr.Server"
RUN dotnet build "./Caskr.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Caskr.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Caskr.Server.dll"]
