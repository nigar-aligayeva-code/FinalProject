# =========================================================
# Tripio Web (ASP.NET Core 8 MVC) — Docker image
# Build: repo kökündən  ->  docker build -f HotelTravel.Web/Dockerfile .
# =========================================================

# ---------- 1. Build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY HotelTravel.Domain/HotelTravel.Domain.csproj           HotelTravel.Domain/
COPY HotelTravel.Application/HotelTravel.Application.csproj HotelTravel.Application/
COPY HotelTravel.Web/HotelTravel.Web.csproj                 HotelTravel.Web/
RUN dotnet restore HotelTravel.Web/HotelTravel.Web.csproj

COPY . .
RUN dotnet publish HotelTravel.Web/HotelTravel.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- 2. Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HotelTravel.Web.dll"]
