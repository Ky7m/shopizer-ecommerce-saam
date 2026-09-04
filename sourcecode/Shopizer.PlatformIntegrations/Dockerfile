FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Shopizer.ServiceDefaults/Shopizer.ServiceDefaults.csproj", "Shopizer.ServiceDefaults/"]
COPY ["Shopizer.PlatformIntegrations/Shopizer.PlatformIntegrations.csproj", "Shopizer.PlatformIntegrations/"]
RUN dotnet restore "Shopizer.PlatformIntegrations/Shopizer.PlatformIntegrations.csproj"

COPY Shopizer.ServiceDefaults/ Shopizer.ServiceDefaults/
COPY Shopizer.PlatformIntegrations/ Shopizer.PlatformIntegrations/
WORKDIR /src/Shopizer.PlatformIntegrations
RUN dotnet publish "Shopizer.PlatformIntegrations.csproj" --configuration Release --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Shopizer.PlatformIntegrations.dll"]
