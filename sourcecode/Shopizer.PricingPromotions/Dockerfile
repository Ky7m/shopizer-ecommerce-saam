FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Shopizer.ServiceDefaults/Shopizer.ServiceDefaults.csproj", "Shopizer.ServiceDefaults/"]
COPY ["Shopizer.PricingPromotions/Shopizer.PricingPromotions.csproj", "Shopizer.PricingPromotions/"]
RUN dotnet restore "Shopizer.PricingPromotions/Shopizer.PricingPromotions.csproj"

COPY Shopizer.ServiceDefaults/ Shopizer.ServiceDefaults/
COPY Shopizer.PricingPromotions/ Shopizer.PricingPromotions/
WORKDIR /src/Shopizer.PricingPromotions
RUN dotnet publish "Shopizer.PricingPromotions.csproj" --configuration Release \
    --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Shopizer.PricingPromotions.dll"]
