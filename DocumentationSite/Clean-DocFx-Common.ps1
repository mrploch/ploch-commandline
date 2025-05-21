cd $PSScriptRoot
Remove-Item bin, obj, _site -Recurse -Force -Confirm:$false
Remove-Item api/*.yml -Exclude toc.yml -Confirm:$false -Force
Remove-Item api/.manifest -Force -Confirm:$false

dotnet restore

if ($error)
{
    Write-Host
    Write-Host "Last error:"
    Write-Host $error
    Read-Host Completed with errors. Press enter.
}