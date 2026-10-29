
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "MisaTrackerr/MisaTrackerr.Server/MisaTrackerr.Server.csproj" -p:SkipClientProject=true

RUN dotnet publish "MisaTrackerr/MisaTrackerr.Server/MisaTrackerr.Server.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:SkipClientProject=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["sh", "-c", "dotnet MisaTrackerr.Server.dll --urls http://0.0.0.0:${PORT:-10000}"]
