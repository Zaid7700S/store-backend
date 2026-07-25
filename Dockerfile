# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["store.csproj", "./"]
RUN dotnet restore "./store.csproj"

# Copy the rest of the code and build it
COPY . .
WORKDIR "/src/."
RUN dotnet publish "store.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Port configuration for .NET 8/9 on Render
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "YourProjectName.dll"]
