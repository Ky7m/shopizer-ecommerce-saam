FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Shopizer.CartCheckout/Shopizer.CartCheckout.csproj", "Shopizer.CartCheckout/"]
COPY ["Shopizer.ServiceDefaults/Shopizer.ServiceDefaults.csproj", "Shopizer.ServiceDefaults/"]
RUN dotnet restore "Shopizer.CartCheckout/Shopizer.CartCheckout.csproj"
COPY . .
WORKDIR "/src/Shopizer.CartCheckout"
RUN dotnet publish "Shopizer.CartCheckout.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Shopizer.CartCheckout.dll"]
