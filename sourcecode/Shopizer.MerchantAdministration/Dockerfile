FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Shopizer.MerchantAdministration/Shopizer.MerchantAdministration.csproj", "Shopizer.MerchantAdministration/"]
COPY ["Shopizer.ServiceDefaults/Shopizer.ServiceDefaults.csproj", "Shopizer.ServiceDefaults/"]
RUN dotnet restore "Shopizer.MerchantAdministration/Shopizer.MerchantAdministration.csproj"
COPY . .
WORKDIR "/src/Shopizer.MerchantAdministration"
RUN dotnet publish "Shopizer.MerchantAdministration.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Shopizer.MerchantAdministration.dll"]
