FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["../samples/HelloWorldConsoleApp/HelloWorldConsoleApp.csproj", "../samples/HelloWorldConsoleApp/"]
RUN dotnet restore "../samples/HelloWorldConsoleApp/HelloWorldConsoleApp.csproj"
COPY . .
WORKDIR "/src/../samples/HelloWorldConsoleApp"
RUN dotnet build "HelloWorldConsoleApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HelloWorldConsoleApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HelloWorldConsoleApp.dll"]
