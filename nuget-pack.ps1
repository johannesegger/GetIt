dotnet publish -c Release GetIt.WPF
dotnet pack GetIt.Controller /p:Configuration=Release /p:PackageOutputPath="$pwd\dist"
