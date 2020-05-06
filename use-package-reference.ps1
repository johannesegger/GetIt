Param(
  [Parameter(Mandatory=$True)][string]$project,
  [Parameter(Mandatory=$True)][string]$version
)

dotnet remove $project reference ..\GetIt.Controller\GetIt.Controller.fsproj
# TODO install https://github.com/microsoft/artifacts-credprovider
dotnet add $project package GetIt -s https://pkgs.dev.azure.com/eggerhansi/_packaging/GetIt@Local/nuget/v3/index.json --interactive -v $version
