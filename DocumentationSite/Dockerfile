FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["DocumentationSite/DocumentationSite.csproj", "DocumentationSite/"]
RUN dotnet restore "DocumentationSite/DocumentationSite.csproj"
COPY . .
WORKDIR "/src/DocumentationSite"
RUN dotnet build "DocumentationSite.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocumentationSite.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocumentationSite.dll"]
