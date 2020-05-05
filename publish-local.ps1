$version = "3.1.4"
dotnet build -c Release .\GetIt.Controller\
dotnet build -c Release .\GetIt.UI.Container\
# Push-Location .\GetIt.UI
# yarn webpack
# Pop-Location
.\.paket\paket.exe pack --minimum-from-lock-file --version $version GetIt.Controller
Move-Item .\GetIt.Controller\GetIt.$version.nupkg .\nuget\
dotnet add $project package GetIt -s "$pwd\nuget" -v $version
