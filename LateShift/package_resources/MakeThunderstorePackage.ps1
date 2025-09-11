
param (
    [string]$ver = "1.0.0",
    [string]$arch = "il2cpp",
    [string]$proj = ""
 )

 # Check params
 if ("$arch" -eq "il2cpp") {
    $arch_str = "IL2CPP"
    $net_ver = "net6"
}
elseif ("$arch" -eq "mono") {
    $arch_str = "Mono"
    $net_ver = "netstandard2.1"
}
else {
    Write-Output 'Specify "-arch il2cpp" or "-arch mono"!'
    Exit -1
}

if ("$proj" -eq "") {
    Write-Output 'Specify "-proj <projectname>""!'
    Exit -1
}

$dll_file = "$($proj)$($arch_str).dll"
$zip_file = "$($proj)_$($arch_str)-$($ver).zip"
$pkg_base = "package\thunderstore\$($arch)"

# Clean and create directory structure
Remove-Item -Recurse -ErrorAction Ignore "$($pkg_base)"
Remove-Item -ErrorAction Ignore "$($pkg_base)\..\$($zip_file)"
mkdir "$($pkg_base)\Mods"

# Copy the files
Copy "bin\Debug\$($net_ver)\$($dll_file)" "$($pkg_base)\Mods"
Copy 'package_resources\icon.png' "$($pkg_base)\icon.png"
Copy 'package_resources\README.md' "$($pkg_base)\README.md"
Copy 'package_resources\manifest.json' "$($pkg_base)\manifest.json"

# Set version and arch strings
$json = [System.IO.File]::ReadAllText("$($pkg_base)\manifest.json")
$json = $json.Replace('%%VERSION%%', $ver)
$json = $json.Replace('%%ARCH%%', $arch_str)
[System.IO.File]::WriteAllText("$($pkg_base)\manifest.json", $json)

# Zip it all up
Compress-Archive -Path "$($pkg_base)\*" -DestinationPath "$($pkg_base)\..\$($zip_file)"
