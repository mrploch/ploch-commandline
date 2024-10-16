.\Clean-Repository.ps1
$sonarToken = $env:PLOCH_DATA_SONAR_TOKEN
dotnet tool install --global dotnet-sonarscanner
dotnet tool install --global dotnet-coverage
dotnet restore Ploch.Data.sln
dotnet sonarscanner begin /k:"mrploch_ploch-data" /o:"mrploch" /d:sonar.login = "$sonarToken" /d:sonar.cs.opencover.reportsPaths = **/CoverageResults/coverage.opencover.xml /d:sonar.host.url = "https://sonarcloud.io"
dotnet build Ploch.Data.sln --no-incremental --no-restore
dotnet test Ploch.Data.sln --verbosity normal --no-build --logger "trx;LogFileName=TestOutputResults.xml" /p:CollectCoverage=true /p:CoverletOutput=./CoverageResults/ "/p:CoverletOutputFormat=cobertura%2copencover"
dotnet  sonarscanner end /d:sonar.login="$sonarToken"
