FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore (cache-friendly)
COPY src/AiDevAssistant.csproj src/
RUN dotnet restore src/AiDevAssistant.csproj

# Copy the rest and publish
COPY src/ src/
RUN dotnet publish src/AiDevAssistant.csproj -c Release -o /app /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user 'app' already exists in base image
COPY --from=build /app .
RUN chown -R app:app /app
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "AiDevAssistant.dll"]