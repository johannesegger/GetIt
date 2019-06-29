Param(
  [Parameter(Mandatory=$True)][string]$project
)

dotnet remove $project package GetIt
dotnet add $project reference ..\GetIt.Controller
