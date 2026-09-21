FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

ARG SEM_VERSION=0.0.0
ARG ASSEMBLY_SEM_VERSION=0.0.0.0
ARG ASSEMBLY_SEM_FILE_VERSION=0.0.0.0

COPY SuperMegaUnboxDeluxe.Api.slnx ./

COPY SuperMegaUnboxDeluxe.Api/*.csproj ./SuperMegaUnboxDeluxe.Api/
COPY SuperMegaUnboxDeluxe.Application/*.csproj ./SuperMegaUnboxDeluxe.Application/
COPY SuperMegaUnboxDeluxe.Domain/*.csproj ./SuperMegaUnboxDeluxe.Domain/
COPY SuperMegaUnboxDeluxe.Common/*.csproj ./SuperMegaUnboxDeluxe.Common/
COPY SuperMegaUnboxDeluxe.Infrastructure/*.csproj ./SuperMegaUnboxDeluxe.Infrastructure/

RUN dotnet restore SuperMegaUnboxDeluxe.Api.slnx \
    /p:RestoreFallbackFolders=

COPY . .

RUN dotnet publish SuperMegaUnboxDeluxe.Api/SuperMegaUnboxDeluxe.Api.csproj \
    -c Release \
    -o /app \
    --no-restore \
    /p:Version=${SEM_VERSION} \
    /p:AssemblyVersion=${ASSEMBLY_SEM_VERSION} \
    /p:FileVersion=${ASSEMBLY_SEM_FILE_VERSION}


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SuperMegaUnboxDeluxe.Api.dll"]
