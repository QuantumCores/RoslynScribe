$destination = "D:\Source\QuantumNugets"
$location = Get-Location
$output = "bin\Debug"

dotnet pack -c Debug


Write-Host '>> packaging' -ForegroundColor blue  
dotnet pack -c Debug

Write-Host '>> copying' -ForegroundColor blue  
$nugetPath = Join-Path -Path $location -ChildPath $output  
Get-ChildItem $nugetPath -Filter *.nupkg | Copy-Item -Destination $destination -Force -PassThru

Write-Host ""
Write-Host ""


Read-Host -Prompt "Press Enter to continue"
cls