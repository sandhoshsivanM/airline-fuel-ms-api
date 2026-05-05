# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first for better layer caching
COPY AirlineFuelMS.slnx ./
COPY AirlineFuelMS.Core/AirlineFuelMS.Core.csproj         AirlineFuelMS.Core/
COPY AirlineFuelMS.Infrastructure/AirlineFuelMS.Infrastructure.csproj AirlineFuelMS.Infrastructure/
COPY AirlineFuelMS.API/AirlineFuelMS.API.csproj           AirlineFuelMS.API/
RUN dotnet restore AirlineFuelMS.API/AirlineFuelMS.API.csproj

# Now bring in the source and publish
COPY AirlineFuelMS.Core/         AirlineFuelMS.Core/
COPY AirlineFuelMS.Infrastructure/ AirlineFuelMS.Infrastructure/
COPY AirlineFuelMS.API/          AirlineFuelMS.API/
RUN dotnet publish AirlineFuelMS.API/AirlineFuelMS.API.csproj \
    -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Render injects PORT (default 10000); the app reads it in Program.cs.
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "AirlineFuelMS.API.dll"]
