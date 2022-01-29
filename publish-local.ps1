$version = "3.2.6-alpha0007"
dotnet publish -r win10-x64 -c Release --no-self-contained -p:DebugType=None .\GetIt.UI.Container
dotnet build -c Release .\GetIt.Controller
.paket\paket.exe pack --build-config Release --minimum-from-lock-file --version $version .\GetIt.Controller
mkdir .\nuget -Force | Out-Null
Move-Item .\GetIt.Controller\GetIt.$version.nupkg .\nuget\ -Force
.\add-aspnetcore-reference.ps1 .\nuget\GetIt.$version.nupkg
