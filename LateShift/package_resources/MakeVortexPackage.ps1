
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

if ("$($proj)" -eq "") {
    Write-Output 'Specify "-proj <projectname>"!'
    Exit -1
}

$zip_file = "$($proj)_$($arch_str)-$($ver).zip"
$dll_file = "$($proj)$($arch_str).dll"
$pkg_base = "package\vortex\$($arch)"

# Clean and create directory structure
Remove-Item -Recurse -ErrorAction Ignore "$($pkg_base)"
Remove-Item -ErrorAction Ignore "$($pkg_base)\..\$($zip_file)"
mkdir "$($pkg_base)\mods"

# Copy the files
Copy "bin\Debug\$($net_ver)\$($dll_file)" "$($pkg_base)\mods"

# Zip it all up
Compress-Archive -Path "$($pkg_base)\*" -DestinationPath "$($pkg_base)\..\$($zip_file)"