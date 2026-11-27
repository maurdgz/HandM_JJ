# API ImportNet para Render. El frontend Astro se publica aparte como Static Site en Render.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY ImportNet.Domain/ImportNet.Domain.csproj ImportNet.Domain/
COPY ImportNet.Application/ImportNet.Application.csproj ImportNet.Application/
COPY ImportNet.Infrastructure/ImportNet.Infrastructure.csproj ImportNet.Infrastructure/
COPY ImportNet.API/ImportNet.API.csproj ImportNet.API/
RUN dotnet restore ImportNet.API/ImportNet.API.csproj
COPY ImportNet.Domain/ ImportNet.Domain/
COPY ImportNet.Application/ ImportNet.Application/
COPY ImportNet.Infrastructure/ ImportNet.Infrastructure/
COPY ImportNet.API/ ImportNet.API/
RUN dotnet publish ImportNet.API/ImportNet.API.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=10000 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "ImportNet.API.dll"]
