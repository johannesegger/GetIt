Param(
  [Parameter(Mandatory=$True)][string]$version
)

Push-Location $PSScriptRoot
dotnet remove reference ..\GetIt.Controller\GetIt.Controller.fsproj
dotnet add package GetIt -s https://pkgs.dev.azure.com/eggerhansi/_packaging/GetIt@Local/nuget/v3/index.json -v $version
Pop-Location
