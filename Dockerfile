FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["SistemaVisionTech/SistemaVisionTech.csproj", "SistemaVisionTech/"]
RUN dotnet restore "SistemaVisionTech/SistemaVisionTech.csproj"

COPY SistemaVisionTech/ SistemaVisionTech/
WORKDIR "/src/SistemaVisionTech"
RUN dotnet publish "SistemaVisionTech.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Crear y usar usuario sin privilegios por seguridad (AppSec)
USER $APP_UID

COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaVisionTech.dll"]
