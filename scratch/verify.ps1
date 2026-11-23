$folders = @("C:\InterviewRoom1_ProgramFiles", "C:\InterviewRoom1\Exported_ProgramFiles")

foreach ($f in $folders) {
    Write-Host "========================================="
    Write-Host "Path: $f"
    $files = Get-ChildItem -Path $f -Recurse -File
    $size = ($files | Measure-Object -Property Length -Sum).Sum / 1MB
    Write-Host ("Total Files: " + $files.Count)
    Write-Host ("Total Size: " + [math]::Round($size, 2) + " MB")
    
    $scripts = Get-ChildItem -Path (Join-Path $f "Source_Scripts_And_Code") -Recurse -Filter *.cs
    Write-Host "Source C# Scripts count: " $scripts.Count
    foreach ($s in $scripts) {
        Write-Host ("  - " + $s.Name + " (" + $s.Length + " bytes)")
    }
}
