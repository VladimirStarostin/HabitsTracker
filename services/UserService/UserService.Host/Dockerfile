FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./services/UserService/ /src/services/UserService
COPY ./Helpers/ /src/Helpers/

RUN dotnet restore services/UserService/UserService.Host/UserService.Host.fsproj
RUN dotnet publish services/UserService/UserService.Host/UserService.Host.fsproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserService.Host.dll"]