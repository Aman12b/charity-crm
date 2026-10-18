# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so the restore layer is cached.
COPY Directory.Build.props ./
COPY src/CharityCrm.Api/CharityCrm.Api.csproj src/CharityCrm.Api/
COPY src/CharityCrm.Core/CharityCrm.Core.csproj src/CharityCrm.Core/
COPY src/CharityCrm.Infrastructure/CharityCrm.Infrastructure.csproj src/CharityCrm.Infrastructure/
RUN dotnet restore src/CharityCrm.Api/CharityCrm.Api.csproj

COPY src/ src/
RUN dotnet publish src/CharityCrm.Api/CharityCrm.Api.csproj -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Fonts for QuestPDF, curl for the container health check.
RUN apt-get update \
    && apt-get install -y --no-install-recommends fontconfig fonts-dejavu-core curl \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /data/invoices /data/keys && chown -R app:app /data

WORKDIR /app
COPY --from=build /app ./

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "CharityCrm.Api.dll"]
