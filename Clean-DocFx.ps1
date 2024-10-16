cd $PSScriptRoot
Remove-Item docfx_project/bin -Force
Remove-Item docfx_project/obj -Force
Remove-Item docfx_project/_site -Force
Remove-Item docfx_project/api/*.yml -Exclude toc.yml -Force
Remove-Item docfx_project/api/.manifest -Force

if ($error)
{
    Write-Host
    Write-Host "Last error:"
    Write-Host $error
    Read-Host Completed with errors. Press enter.
}