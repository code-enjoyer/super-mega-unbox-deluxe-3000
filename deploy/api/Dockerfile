FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
WORKDIR /src
COPY global.json ./
COPY backend/ backend/
RUN dotnet restore backend/Api/SuperMegaUnboxDeluxe.Api.csproj --locked-mode
ARG SEM_VERSION=0.0.0
ARG ASSEMBLY_SEM_VERSION=0.0.0.0
ARG ASSEMBLY_SEM_FILE_VERSION=0.0.0.0
RUN dotnet publish backend/Api/SuperMegaUnboxDeluxe.Api.csproj -c Release -o /app --no-restore \
    /p:Version=${SEM_VERSION} /p:AssemblyVersion=${ASSEMBLY_SEM_VERSION} /p:FileVersion=${ASSEMBLY_SEM_FILE_VERSION}

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
COPY --from=build /app ./
RUN mkdir -p /app/logs && chown "$APP_UID" /app/logs
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "SuperMegaUnboxDeluxe.Api.dll"]
