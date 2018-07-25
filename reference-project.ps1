Push-Location .\src\PlayAndLearn
dotnet remove package Elmish.Net
dotnet add reference ..\..\..\Elmish.Net\Elmish.Net\Elmish.Net.csproj
Pop-Location
dotnet sln add ..\Elmish.Net\Elmish.Net\Elmish.Net.csproj