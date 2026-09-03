FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Shopizer.CatalogProduct/Shopizer.CatalogProduct.csproj", "Shopizer.CatalogProduct/"]
COPY ["Shopizer.ServiceDefaults/Shopizer.ServiceDefaults.csproj", "Shopizer.ServiceDefaults/"]
RUN dotnet restore "Shopizer.CatalogProduct/Shopizer.CatalogProduct.csproj"
COPY . .
WORKDIR "/src/Shopizer.CatalogProduct"
RUN dotnet publish "Shopizer.CatalogProduct.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Shopizer.CatalogProduct.dll"]
