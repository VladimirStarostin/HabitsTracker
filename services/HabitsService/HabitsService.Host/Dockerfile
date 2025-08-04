FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./services/HabitsService/ /src/services/HabitsService
COPY ./Helpers/ /src/Helpers/

RUN dotnet restore services/HabitsService/HabitsService.Host/HabitsService.Host.fsproj
RUN dotnet publish services/HabitsService/HabitsService.Host/HabitsService.Host.fsproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HabitsService.Host.dll"]