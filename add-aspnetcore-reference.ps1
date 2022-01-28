param (
    [string]$nupkgPath
)

[System.Reflection.Assembly]::LoadWithPartialName("System.IO.Compression") | Out-Null

$nupkgPath = Resolve-Path $nupkgPath
$zipBytes = [System.IO.File]::ReadAllBytes($nupkgPath)
$zipStream = New-Object System.IO.MemoryStream # create expandable memory stream
$tempZipStream = New-Object System.IO.MemoryStream(,$zipBytes)
$tempZipStream.CopyTo($zipStream)
$zipStream.Position = 0
$zipArchive = New-Object System.IO.Compression.ZipArchive($zipStream, [System.IO.Compression.ZipArchiveMode]::Update, $true)
$zipEntry = $zipArchive.GetEntry("GetIt.nuspec")

$entryStream = $zipEntry.Open()
$entryReader = New-Object System.IO.StreamReader($entryStream)
$nuspec = [xml]$entryReader.ReadToEnd()
$entryReader.Close()
$namespaceManager = New-Object System.Xml.XmlNamespaceManager($nuspec.NameTable)
$namespace = "http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd"
$namespaceManager.AddNamespace("x", "http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd")
$metadata = $nuspec.SelectSingleNode("/x:package/x:metadata", $namespaceManager)

$dependencies = $metadata.SelectSingleNode("x:dependencies", $namespaceManager)
$dependencyGroup = $nuspec.CreateElement("group", $namespace)
$dependencyGroup.SetAttribute("targetFramework", "net6.0") | Out-Null
while ($dependencies.FirstChild) {
    $dependencyGroup.AppendChild($dependencies.FirstChild) | Out-Null
}
$dependencies.AppendChild($dependencyGroup) | Out-Null

$frameworkReference = $nuspec.CreateElement("frameworkReference", $namespace)
$frameworkReference.SetAttribute("name", "Microsoft.AspNetCore.App") | Out-Null
$frameworkReferenceGroup = $nuspec.CreateElement("group", $namespace)
$frameworkReferenceGroup.SetAttribute("targetFramework", "net6.0") | Out-Null
$frameworkReferenceGroup.AppendChild($frameworkReference) | Out-Null
$frameworkReferences = $nuspec.CreateElement("frameworkReferences", $namespace)
$frameworkReferences.AppendChild($frameworkReferenceGroup) | Out-Null
$metadata.AppendChild($frameworkReferences) | Out-Null

$formattedXmlStream = New-Object System.IO.MemoryStream
$formattedXmlStreamWriter = New-Object System.IO.StreamWriter($formattedXmlStream)
$formattedXmlWriter = New-Object System.Xml.XmlTextWriter($formattedXmlStreamWriter)
$formattedXmlWriter.Formatting = [System.Xml.Formatting]::Indented
$nuspec.Save($formattedXmlWriter)
$formattedXmlStream.Position = 0

$entryStream = $zipEntry.Open()
$entryStream.SetLength($formattedXmlStream.Length)
$formattedXmlStream.CopyTo($entryStream)
$entryStream.Close()

$zipArchive.Dispose()

[System.IO.File]::WriteAllBytes($nupkgPath, $zipStream.ToArray())
