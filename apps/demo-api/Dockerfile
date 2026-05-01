# ── Multi-stage build for self-contained .NET 10 API ──

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore (layer caching)
COPY src/DemoApi/DemoApi.csproj .
RUN dotnet restore

# Copy source and build
COPY src/DemoApi/ .
RUN dotnet publish -c Release -o /app/publish --self-contained false --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system app && adduser --system --ingroup app app
USER app

# Copy published app
COPY --from=build /app/publish .

# Metadata
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV APP_PORT=8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "DemoApi.dll"]
