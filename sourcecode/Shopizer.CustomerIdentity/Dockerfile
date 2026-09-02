FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Shopizer.ServiceDefaults/Shopizer.ServiceDefaults.csproj", "Shopizer.ServiceDefaults/"]
COPY ["Shopizer.CustomerIdentity/Shopizer.CustomerIdentity.csproj", "Shopizer.CustomerIdentity/"]
RUN dotnet restore "Shopizer.CustomerIdentity/Shopizer.CustomerIdentity.csproj"

COPY Shopizer.ServiceDefaults/ Shopizer.ServiceDefaults/
COPY Shopizer.CustomerIdentity/ Shopizer.CustomerIdentity/
WORKDIR /src/Shopizer.CustomerIdentity
RUN dotnet publish "Shopizer.CustomerIdentity.csproj" \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Shopizer.CustomerIdentity.dll"]
