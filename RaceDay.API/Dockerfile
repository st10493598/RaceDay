# Stage 1: Build the RaceDay API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy the API project
COPY RaceDay.API/ RaceDay.API/

# Restore NuGet packages
RUN dotnet restore RaceDay.API/RaceDay.API.csproj

# Publish the application
RUN dotnet publish RaceDay.API/RaceDay.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Run the RaceDay API
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RaceDay.API.dll"]