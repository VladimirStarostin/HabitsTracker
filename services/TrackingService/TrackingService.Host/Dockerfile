FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./services/TrackingService/ /src/services/TrackingService
COPY ./Helpers/ /src/Helpers/

RUN dotnet restore services/TrackingService/TrackingService.Host/TrackingService.Host.fsproj
RUN dotnet publish services/TrackingService/TrackingService.Host/TrackingService.Host.fsproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TrackingService.Host.dll"]