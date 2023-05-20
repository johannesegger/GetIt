$version = "3.3.0-alpha0004"
dotnet pack --configuration Release --output .\nuget -p:PackageVersion=$version .\GetIt.Controller
