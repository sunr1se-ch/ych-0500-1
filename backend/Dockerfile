FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PlateTracking.csproj", "./"]
RUN dotnet restore "PlateTracking.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "PlateTracking.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PlateTracking.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /data
EXPOSE 5000
ENTRYPOINT ["dotnet", "PlateTracking.dll"]
