FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/TricalRevive.Api/TricalRevive.Api.csproj", "src/TricalRevive.Api/"]
COPY ["src/TricalRevive.GrainInterfaces/TricalRevive.GrainInterfaces.csproj", "src/TricalRevive.GrainInterfaces/"]
RUN dotnet restore "src/TricalRevive.Api/TricalRevive.Api.csproj"

COPY src/TricalRevive.Api/ src/TricalRevive.Api/
COPY src/TricalRevive.GrainInterfaces/ src/TricalRevive.GrainInterfaces/

WORKDIR /src/src/TricalRevive.Api
RUN dotnet publish "TricalRevive.Api.csproj" -c Release -o /app --self-contained false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TricalRevive.Api.dll"]