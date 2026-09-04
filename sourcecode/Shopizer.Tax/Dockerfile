FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Shopizer.Tax/Shopizer.Tax.csproj", "Shopizer.Tax/"]
COPY ["Shopizer.ServiceDefaults/Shopizer.ServiceDefaults.csproj", "Shopizer.ServiceDefaults/"]
RUN dotnet restore "Shopizer.Tax/Shopizer.Tax.csproj"
COPY . .
WORKDIR "/src/Shopizer.Tax"
RUN dotnet restore "Shopizer.Tax.csproj" --force
RUN dotnet build "Shopizer.Tax.csproj" -c Release --no-restore -o /app/build
RUN dotnet publish "Shopizer.Tax.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Shopizer.Tax.dll"]
