FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/TricalRevive.Silo/TricalRevive.Silo.csproj", "src/TricalRevive.Silo/"]
COPY ["src/TricalRevive.Grains/TricalRevive.Grains.csproj", "src/TricalRevive.Grains/"]
COPY ["src/TricalRevive.GrainInterfaces/TricalRevive.GrainInterfaces.csproj", "src/TricalRevive.GrainInterfaces/"]
RUN dotnet restore "src/TricalRevive.Silo/TricalRevive.Silo.csproj"

COPY src/TricalRevive.Silo/ src/TricalRevive.Silo/
COPY src/TricalRevive.Grains/ src/TricalRevive.Grains/
COPY src/TricalRevive.GrainInterfaces/ src/TricalRevive.GrainInterfaces/

WORKDIR /src/src/TricalRevive.Silo
RUN dotnet publish "TricalRevive.Silo.csproj" -c Release -o /app --self-contained false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "TricalRevive.Silo.dll"]