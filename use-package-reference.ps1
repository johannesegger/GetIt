Param(
  [Parameter(Mandatory=$True)][string]$project,
  [Parameter(Mandatory=$True)][string]$version
)

dotnet remove $project reference ..\GetIt.Controller\GetIt.Controller.fsproj
dotnet add $project package GetIt -s $PSScriptRoot\nuget -v $version
