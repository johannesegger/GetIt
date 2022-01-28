$version = "3.2.4-alpha0008"
dotnet publish -r win10-x86 -c Release --self-contained false -p:PublishSingleFile=True -p:DebugType=None .\GetIt.UI.Container
dotnet publish .\GetIt.Controller
.\.paket\paket.exe pack --build-config Debug --minimum-from-lock-file --version $version .\GetIt.Controller
mkdir .\nuget -Force | Out-Null
Move-Item .\GetIt.Controller\GetIt.$version.nupkg .\nuget\ -Force
.\add-aspnetcore-reference.ps1 .\nuget\GetIt.$version.nupkg
