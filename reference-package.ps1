Param(
  [Parameter(Mandatory=$True)][string]$version
)

Push-Location .\src\PlayAndLearn
dotnet remove reference ..\..\..\Elmish.Net\Elmish.Net\Elmish.Net.csproj
dotnet nuget locals http-cache -c
dotnet add package Elmish.Net --version $version
Pop-Location
dotnet sln remove ..\Elmish.Net\Elmish.Net\Elmish.Net.csproj