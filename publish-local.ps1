$version = "3.1.5"
dotnet publish -r win10-x86 -c Release --self-contained false -p:PublishSingleFile=True -p:DebugType=None .\GetIt.UI.Container
.\.paket\paket.exe pack --minimum-from-lock-file --version $version .\GetIt.Controller
mkdir .\nuget -Force | Out-Null
Move-Item .\GetIt.Controller\GetIt.$version.nupkg .\nuget\ -Force
.\add-aspnetcore-reference.ps1 .\nuget\GetIt.$version.nupkg
dotnet add .\GetIt.Sample package GetIt -s "$pwd\nuget" -v $version
