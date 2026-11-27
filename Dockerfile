# Stage 1: Build Astro + React frontend
FROM node:24-alpine AS frontend
WORKDIR /frontend
COPY ImportNet/frontend/package*.json ./
RUN npm ci
COPY ImportNet/frontend/ ./
RUN npm run build

# Stage 2: Build & publish .NET 10 API with embedded frontend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ImportNet/Directory.Build.props ImportNet/
COPY ImportNet/.editorconfig ImportNet/
COPY ImportNet/ImportNet.Domain/ImportNet.Domain.csproj ImportNet/ImportNet.Domain/
COPY ImportNet/ImportNet.Application/ImportNet.Application.csproj ImportNet/ImportNet.Application/
COPY ImportNet/ImportNet.Infrastructure/ImportNet.Infrastructure.csproj ImportNet/ImportNet.Infrastructure/
COPY ImportNet/ImportNet.API/ImportNet.API.csproj ImportNet/ImportNet.API/
RUN dotnet restore ImportNet/ImportNet.API/ImportNet.API.csproj

COPY ImportNet/ImportNet.Domain/ ImportNet/ImportNet.Domain/
COPY ImportNet/ImportNet.Application/ ImportNet/ImportNet.Application/
COPY ImportNet/ImportNet.Infrastructure/ ImportNet/ImportNet.Infrastructure/
COPY ImportNet/ImportNet.API/ ImportNet/ImportNet.API/
COPY --from=frontend /frontend/dist/ ImportNet/ImportNet.API/wwwroot/
RUN dotnet publish ImportNet/ImportNet.API/ImportNet.API.csproj --configuration Release --no-restore --output /app

# Stage 3: Non-root ASP.NET Core 10 runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_ENVIRONMENT=Production \
    PORT=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ImportNet.API.dll"]
