# Use the official .NET 8 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["ProxyServer.csproj", "./"]
RUN dotnet restore "ProxyServer.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet build "ProxyServer.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ProxyServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET 8 runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port 80
EXPOSE 80

# Set the entry point
ENTRYPOINT ["dotnet", "ProxyServer.dll"]