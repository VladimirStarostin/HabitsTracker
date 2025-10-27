FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./gateway/HabitsTracker.Gateway.sln ./gateway/
COPY ./gateway/HabitsTracker.Gateway.Host/HabitsTracker.Gateway.Host.fsproj ./gateway/HabitsTracker.Gateway.Host/
COPY ./gateway/HabitsTracker.Gateway.Api/HabitsTracker.Gateway.Api.fsproj ./gateway/HabitsTracker.Gateway.Api/
COPY ./services/HabitsService/HabitsService.Protos/ ./services/HabitsService/HabitsService.Protos/
COPY ./services/TrackingService/TrackingService.Protos/ ./services/TrackingService/TrackingService.Protos/
COPY ./services/UserService/UserService.Protos/ ./services/UserService/UserService.Protos/
COPY ./Helpers/Helpers.fsproj ./Helpers/

RUN dotnet restore  ./gateway/HabitsTracker.Gateway.Host/HabitsTracker.Gateway.Host.fsproj -r linux-x64

COPY ./gateway ./gateway
COPY ./services/HabitsService/HabitsService.Protos/ ./services/HabitsService/
COPY ./services/UserService/UserService.Protos/ ./services/UserService/
COPY ./services/HabitsService/HabitsService.Protos/ ./services/HabitsService/
COPY ./Helpers ./Helpers/

RUN dotnet publish gateway/HabitsTracker.Gateway.Host/HabitsTracker.Gateway.Host.fsproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HabitsTracker.Gateway.Host.dll"]