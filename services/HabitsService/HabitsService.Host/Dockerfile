FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./services/HabitsService/HabitsService.sln ./services/HabitsService/
COPY ./services/HabitsService/HabitsService.Host/HabitsService.Host.fsproj ./services/HabitsService/HabitsService.Host/
COPY ./services/HabitsService/HabitsService.Protos/HabitsService.Protos.csproj ./services/HabitsService/HabitsService.Protos/
COPY ./services/HabitsService/HabitsService.Migrations/HabitsService.Migrations.fsproj ./services/HabitsService/HabitsService.Migrations/
COPY ./Helpers/Helpers.fsproj ./Helpers/

RUN dotnet restore services/HabitsService/HabitsService.Host/HabitsService.Host.fsproj -r linux-x64

COPY ./services/HabitsService ./services/HabitsService/
COPY ./Helpers ./Helpers/

RUN dotnet publish services/HabitsService/HabitsService.Host/HabitsService.Host.fsproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HabitsService.Host.dll"]