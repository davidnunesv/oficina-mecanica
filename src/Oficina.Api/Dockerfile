
# Etapa 1: compilar e publicar.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore src/Oficina.Api/Oficina.Api.csproj
RUN dotnet restore src/Oficina.DbMigrator/Oficina.DbMigrator.csproj

RUN dotnet publish src/Oficina.Api/Oficina.Api.csproj \
    -c Release \
    --no-restore \
    -o /publish/api \
    /p:UseAppHost=false

RUN dotnet publish src/Oficina.DbMigrator/Oficina.DbMigrator.csproj \
    -c Release \
    --no-restore \
    -o /publish/migrator \
    /p:UseAppHost=false

# Etapa 2: imagem final sem o SDK.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /publish/api/ ./
COPY --from=build /publish/migrator/ /migrator/

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

USER app

CMD ["dotnet", "Oficina.Api.dll"]
