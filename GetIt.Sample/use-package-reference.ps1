Param(
  [Parameter(Mandatory=$True)][string]$version
)

Push-Location $PSScriptRoot
dotnet remove reference ..\GetIt.Controller\GetIt.Controller.fsproj
dotnet add package GetIt -s $pwd\..\dist -v $version
Pop-Location
