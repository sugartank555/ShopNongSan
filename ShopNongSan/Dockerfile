# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution và project file
COPY ShopNongSan.sln ./
COPY ShopNongSan/ShopNongSan.csproj ShopNongSan/

# Restore dependencies
RUN dotnet restore

# Copy toàn bộ source code
COPY . .

# Build và publish
WORKDIR /src/ShopNongSan
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy file đã publish từ stage build
COPY --from=build /app/publish .

# Render dùng PORT env variable
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

EXPOSE 8080

ENTRYPOINT ["dotnet", "ShopNongSan.dll"]
