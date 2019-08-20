Push-Location $PSScriptRoot
try {
    yarn dev $args
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
