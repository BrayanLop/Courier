# syntax=docker/dockerfile:1

# ---- Etapa de compilacion -------------------------------------------------
# Imagen con el SDK completo solo para restaurar y publicar; no llega a produccion.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Se copian primero los .csproj para aprovechar la cache de capas: mientras las
# dependencias no cambien, el restore no se vuelve a ejecutar.
COPY src/CourierMax.Domain/CourierMax.Domain.csproj         src/CourierMax.Domain/
COPY src/CourierMax.Application/CourierMax.Application.csproj src/CourierMax.Application/
COPY src/CourierMax.Infrastructure/CourierMax.Infrastructure.csproj src/CourierMax.Infrastructure/
COPY src/CourierMax.Api/CourierMax.Api.csproj               src/CourierMax.Api/
RUN dotnet restore src/CourierMax.Api/CourierMax.Api.csproj

# Ahora si el resto del codigo y publicacion en modo Release.
COPY src/ src/
RUN dotnet publish src/CourierMax.Api/CourierMax.Api.csproj \
    -c Release -o /app/publish --no-restore

# ---- Etapa de ejecucion ---------------------------------------------------
# Imagen runtime (sin SDK) -> superficie de ataque mas pequenia.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# La imagen aspnet:8.0 trae un usuario no-root llamado "app"; ejecutar como tal
# evita correr el proceso como root dentro del contenedor (buena practica de seguridad).
USER app

# Kestrel escucha en 8080 (HTTP) dentro del contenedor.
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "CourierMax.Api.dll"]
