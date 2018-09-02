param(
    [Parameter(Mandatory=$true)][string]$version,
    [Parameter(Mandatory=$true)][string[]]$releaseNotes
)

. .\set-secrets.ps1
$env:release_version = $version
$env:release_notes = [System.String]::Join([System.Environment]::NewLine, $releaseNotes)
.\fake.cmd build -t GitHubRelease