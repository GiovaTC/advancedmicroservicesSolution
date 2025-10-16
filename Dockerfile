# ============================================================
# 🧱 Etapa base de build (para todos los proyectos)
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de la solución y restaurar dependencias
COPY ./src/ApiGateway/ApiGateway.csproj ./src/ApiGateway/
COPY ./src/ProductService/ProductService.csproj ./src/ProductService/
COPY ./src/Shared/*.csproj ./src/Shared/
RUN dotnet restore ./src/ApiGateway/ApiGateway.csproj
RUN dotnet restore ./src/ProductService/ProductService.csproj

# Copiar todo el código fuente
COPY . .

# ============================================================
# 🧩 Etapa build: ApiGateway
# ============================================================
FROM build AS build-apigateway
RUN dotnet publish ./src/ApiGateway/ApiGateway.csproj -c Release -o /app/api

# ============================================================
# 🧩 Etapa build: ProductService
# ============================================================
FROM build AS build-productservice
RUN dotnet publish ./src/ProductService/ProductService.csproj -c Release -o /app/product

# ============================================================
# 🚀 Etapa runtime base
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Puerto por defecto
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Variable para elegir cuál servicio ejecutar
ARG SERVICE=none

# Copiar el servicio correspondiente
COPY --from=build-apigateway /app/api ./api
COPY --from=build-productservice /app/product ./product

# Selección dinámica del microservicio
ENTRYPOINT ["sh", "-c", "if [ \"$SERVICE\" = 'apigateway' ]; then dotnet ./api/ApiGateway.dll; elif [ \"$SERVICE\" = 'productservice' ]; then dotnet ./product/ProductService.dll; else echo '❌ Debes definir SERVICE=apigateway o productservice'; fi"]
