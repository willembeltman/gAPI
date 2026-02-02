FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# copy csproj + restore
COPY gAPI.Fabric/gAPI.FabricNode.csproj gAPI.Fabric/
RUN dotnet restore gAPI.Fabric/gAPI.FabricNode.csproj

# copy rest + build
COPY . .
WORKDIR /src/gAPI.Fabric
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

# runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0-preview
WORKDIR /app

COPY --from=build /app .

# TCP server luistert zelf op 9494
EXPOSE 9494

ENTRYPOINT ["dotnet", "gAPI.FabricNode.dll"]
