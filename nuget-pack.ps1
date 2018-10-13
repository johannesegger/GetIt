Param(
  [Parameter(Mandatory=$True)][string]$version
)

Push-Location $PSScriptRoot
$outputPath = "$pwd\dist"
dotnet pack src\GetIt /p:Version=$version /p:PackageOutputPath=$outputPath
Pop-Location