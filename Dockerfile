# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY RestaurantReservation.sln .
COPY RestaurantReservation.Web/RestaurantReservation.Web.csproj RestaurantReservation.Web/
COPY RestaurantReservation.Tests/RestaurantReservation.Tests.csproj RestaurantReservation.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
RUN dotnet publish RestaurantReservation.Web/RestaurantReservation.Web.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create uploads directory
RUN mkdir -p /app/wwwroot/uploads/restaurants \
    /app/wwwroot/uploads/branches \
    /app/wwwroot/uploads/tables \
    /app/wwwroot/uploads/menus \
    /app/wwwroot/uploads/qrcodes

# Copy published app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "RestaurantReservation.Web.dll"]
