# Multi-stage Dockerfile for ASP.NET Core 8
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ProductStore.sln", "./"]
COPY ["ProductStore/ProductStore.csproj", "ProductStore/"]
RUN dotnet restore "ProductStore/ProductStore.csproj"

# Copy full source and publish
COPY . .
WORKDIR "/src/ProductStore"
RUN dotnet publish "ProductStore.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose HTTP port (Render uses PORT env variable or port 80/8080)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ProductStore.dll"]
