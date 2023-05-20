$version = "3.2.6-alpha0007"
dotnet build -c Release .\GetIt.Controller
.paket\paket.exe pack --build-config Release --minimum-from-lock-file --version $version .\GetIt.Controller
mkdir .\nuget -Force | Out-Null
Move-Item .\GetIt.Controller\GetIt.$version.nupkg .\nuget\ -Force
.\fix-nupkg.ps1 .\nuget\GetIt.$version.nupkg
