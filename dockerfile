FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Leverage Docker layer caching: copy only the project file and restore first
# so NuGet packages are cached unless the project file changes
COPY OrbitalWatch.Api/OrbitalWatch.Api.csproj OrbitalWatch.Api/
RUN dotnet restore OrbitalWatch.Api/OrbitalWatch.Api.csproj

# Copy the rest of the source and publish
COPY OrbitalWatch.Api/ OrbitalWatch.Api/
RUN dotnet publish OrbitalWatch.Api/OrbitalWatch.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as a non-root user for better security
RUN useradd -m -d /home/appuser appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "OrbitalWatch.Api.dll"]
