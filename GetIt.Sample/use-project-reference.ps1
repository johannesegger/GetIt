Push-Location $PSScriptRoot
dotnet remove package GetIt
dotnet add reference ..\GetIt.Controller
Pop-Location
