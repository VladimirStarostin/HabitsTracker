FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./services/TrackingService/TrackingService.sln ./services/TrackingService/
COPY ./services/TrackingService/TrackingService.Host/TrackingService.Host.fsproj ./services/TrackingService/TrackingService.Host/
COPY ./services/TrackingService/TrackingService.Protos/TrackingService.Protos.csproj ./services/TrackingService/TrackingService.Protos/
COPY ./services/TrackingService/TrackingService.Migrations/TrackingService.Migrations.fsproj ./services/TrackingService/TrackingService.Migrations/
COPY ./Helpers/Helpers.fsproj ./Helpers/

RUN dotnet restore ./services/TrackingService/TrackingService.Host/TrackingService.Host.fsproj -r linux-x64

COPY ./services/TrackingService ./services/TrackingService/
COPY ./Helpers ./Helpers/

RUN dotnet publish services/TrackingService/TrackingService.Host/TrackingService.Host.fsproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TrackingService.Host.dll"]