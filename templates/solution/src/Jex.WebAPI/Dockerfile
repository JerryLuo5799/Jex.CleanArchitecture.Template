# Build context is the solution root:  docker build -f src/Jex.WebAPI/Dockerfile -t TPLNAMELOWER .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a project file changes.
COPY global.json Directory.Build.props Directory.Packages.props nuget.config ./
COPY src/Jex.Domain/Jex.Domain.csproj src/Jex.Domain/
COPY src/Jex.Application/Jex.Application.csproj src/Jex.Application/
COPY src/Jex.Infrastructure/Jex.Infrastructure.csproj src/Jex.Infrastructure/
COPY src/Jex.ServiceDefaults/Jex.ServiceDefaults.csproj src/Jex.ServiceDefaults/
COPY src/Jex.WebAPI/Jex.WebAPI.csproj src/Jex.WebAPI/
RUN dotnet restore src/Jex.WebAPI/Jex.WebAPI.csproj

COPY . .
RUN dotnet publish src/Jex.WebAPI/Jex.WebAPI.csproj -c Release -o /app --no-restore -p:OpenApiGenerateDocumentsOnBuild=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
#if (UseSqlite)
# SQLite file lives on a volume the non-root user can write to.
RUN mkdir -p /data && chown app:app /data
VOLUME /data
ENV ConnectionStrings__DefaultConnection="Data Source=/data/TPLNAMELOWER.db"
#endif
USER app
EXPOSE 8080
COPY --from=build /app .
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD ["dotnet", "Jex.WebAPI.dll", "health-probe"]
ENTRYPOINT ["dotnet", "Jex.WebAPI.dll"]
