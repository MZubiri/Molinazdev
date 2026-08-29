# syntax=docker/dockerfile:1

# Stage 1: Build Astro Frontend
FROM node:20-alpine AS build-frontend
WORKDIR /app/frontend

COPY frontend/package*.json ./
RUN npm install

COPY frontend/ ./
RUN npm run build

# Stage 2: Restore .NET Dependencies
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY ["DigitalServices.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/DigitalServices.Domain/DigitalServices.Domain.csproj", "src/DigitalServices.Domain/"]
COPY ["src/DigitalServices.Application/DigitalServices.Application.csproj", "src/DigitalServices.Application/"]
COPY ["src/DigitalServices.Infrastructure/DigitalServices.Infrastructure.csproj", "src/DigitalServices.Infrastructure/"]
COPY ["src/DigitalServices.Web/DigitalServices.Web.csproj", "src/DigitalServices.Web/"]
RUN dotnet restore "src/DigitalServices.Web/DigitalServices.Web.csproj"

# Stage 3: Build .NET Backend
FROM restore AS build
COPY src/ src/
RUN dotnet build "src/DigitalServices.Web/DigitalServices.Web.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/build

# Stage 4: Publish .NET Backend & Bundle Frontend
FROM build AS publish
RUN dotnet publish "src/DigitalServices.Web/DigitalServices.Web.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# Copy compiled Astro frontend into ASP.NET Core wwwroot
COPY --from=build-frontend /app/frontend/dist /app/publish/wwwroot

# Stage 5: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=publish --chown=app:app /app/publish .

USER app
ENTRYPOINT ["dotnet", "DigitalServices.Web.dll"]
