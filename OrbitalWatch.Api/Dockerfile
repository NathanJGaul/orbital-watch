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
WORKDIR /app

# Create a non-root user - running as root in a container is a common
# finding in security reviews, including DoD ATO assessments.
RUN adduser --disabled-password --gecos "" appuser
COPY --from=build /app/publish .
USER appuser

ENV ASPNETCORE_URLS=httpL//+:8000
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrbitalWatch.Api.dll"]