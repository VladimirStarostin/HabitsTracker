FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./services/UserService/UserService.sln ./services/UserService/
COPY ./services/UserService/UserService.Host/UserService.Host.fsproj ./services/UserService/UserService.Host/
COPY ./services/UserService/UserService.Protos/UserService.Protos.csproj ./services/UserService/UserService.Protos/
COPY ./services/UserService/UserService.Migrations/UserService.Migrations.fsproj ./services/UserService/UserService.Migrations/
COPY ./Helpers/Helpers.fsproj ./Helpers/

RUN dotnet restore services/UserService/UserService.Host/UserService.Host.fsproj -r linux-x64

COPY ./services/UserService/ ./services/UserService
COPY ./Helpers/ ./Helpers/

RUN dotnet publish services/UserService/UserService.Host/UserService.Host.fsproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.Host.dll"]