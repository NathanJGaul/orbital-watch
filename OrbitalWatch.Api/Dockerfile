# -- Build Stage --
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy only the csproj first so Docker can cache the restore layer
# reparately from source changes - restore is the slow step.
COPY OrbitalWatch.Api.csproj .
RUN dotnet restore

# Now copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# -- Runtime stage --
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Use microcheck for distroless friendly healthcheck
# https://github.com/tarampampam/microcheck
COPY --from=ghcr.io/tarampampam/microcheck /bin/httpcheck /bin/httpcheck
#HEALTHCHECK CMD ["httpcheck", "http://localhost:8080/"]

WORKDIR /app

# The dotnet/aspnet image ships with a built-in 'app' user.
# Ensure /data exists so the SQLite volume mount is writable.
RUN mkdir -p /data && chown app:app /data
COPY --from=build /app/publish .
USER app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrbitalWatch.Api.dll"]