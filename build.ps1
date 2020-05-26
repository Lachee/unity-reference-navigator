function RunUnityExporter([string] $install_dir, [string]  $source, [string] $output) 
{    
    Write-Output "Installing Discord Unity Exporter...";
    mkdir -Force "$install_dir";

    git clone https://github.com/Lachee/Unity-Package-Exporter.git "$install_dir"  2>&1 | % { $_.ToString() }
    dotnet build "$install_dir\\UnityPackageExporter";

    Write-Output "Exporting...";
    Write-Output "$install_dir";
    Write-Output "$source";
    Write-Output "$output";
    dotnet run --project "$install_dir\\UnityPackageExporter" -project "$source" -output "$output" -a
}

#powershell -ExecutionPolicy ByPass -File
Write-Host "=========== Building Unity3D Package ===========" -ForegroundColor Green

$libdir = "./";


Write-Output "Exporting Unity Package..."
RunUnityExporter "tools/unity-package-exporter" "$libdir" "ReferenceNavigator.unitypackage"