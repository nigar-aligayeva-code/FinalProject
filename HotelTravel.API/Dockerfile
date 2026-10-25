# =========================================================
# Tripio API (ASP.NET Core 8) — Docker image
# Build: repo kökündən  ->  docker build -f HotelTravel.API/Dockerfile .
# =========================================================

# ---------- 1. Build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Əvvəlcə yalnız .csproj faylları: paketlər dəyişməyibsə restore keşdən gəlir (sürətli build)
COPY HotelTravel.Domain/HotelTravel.Domain.csproj                 HotelTravel.Domain/
COPY HotelTravel.Application/HotelTravel.Application.csproj       HotelTravel.Application/
COPY HotelTravel.Infrastructure/HotelTravel.Infrastructure.csproj HotelTravel.Infrastructure/
COPY HotelTravel.API/HotelTravel.API.csproj                       HotelTravel.API/
RUN dotnet restore HotelTravel.API/HotelTravel.API.csproj

COPY . .
RUN dotnet publish HotelTravel.API/HotelTravel.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- 2. Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# QuestPDF (PDF vauçer) Linux-da şriftlər üçün fontconfig istəyir
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Otel/otaq şəkilləri MVC-nin wwwroot qovluğundadır; seeder onları yoxlayır (Media:WebRootPath)
COPY --from=build /src/HotelTravel.Web/wwwroot ./webroot

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "HotelTravel.API.dll"]
