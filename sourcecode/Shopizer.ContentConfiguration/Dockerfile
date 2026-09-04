FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Shopizer.ContentConfiguration/Shopizer.ContentConfiguration.csproj", "Shopizer.ContentConfiguration/"]
COPY ["Shopizer.ServiceDefaults/Shopizer.ServiceDefaults.csproj", "Shopizer.ServiceDefaults/"]
RUN dotnet restore "Shopizer.ContentConfiguration/Shopizer.ContentConfiguration.csproj"
COPY . .
WORKDIR "/src/Shopizer.ContentConfiguration"
RUN dotnet publish "Shopizer.ContentConfiguration.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Shopizer.ContentConfiguration.dll"]
