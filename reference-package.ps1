Param(
  [Parameter(Mandatory=$True)][string]$version
)

dotnet remove .\src\GetIt reference ..\Elmish.Net\Elmish.Net\Elmish.Net.csproj
dotnet nuget locals http-cache -c
dotnet add .\src\GetIt package Elmish.Net --version $version
dotnet sln .\src remove ..\Elmish.Net\Elmish.Net\Elmish.Net.csproj