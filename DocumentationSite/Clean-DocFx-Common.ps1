<#
.SYNOPSIS
    Removes the generated DocFX output so the next build starts from a clean state.

.DESCRIPTION
    Deletes the rendered site and the API metadata DocFX regenerates from the
    Ploch.CommandLine.Spectre projects. api/toc.yml is authored by hand and is kept.

    Run this when renamed or removed public types leave stale pages behind: DocFX
    overwrites the metadata it regenerates but does not delete files whose source
    symbol no longer exists.
#>
[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'

Push-Location $PSScriptRoot
try
{
    # -ErrorAction Ignore rather than a Test-Path guard: a clean tree is the expected
    # state on a fresh clone, and absence is not a failure worth reporting.
    Remove-Item -Path '_site' -Recurse -Force -Confirm:$false -ErrorAction Ignore

    # toc.yml is hand-authored; everything else under api/ is DocFX metadata output.
    Remove-Item -Path 'api/*.yml' -Exclude 'toc.yml' -Force -Confirm:$false -ErrorAction Ignore
    Remove-Item -Path 'api/.manifest' -Force -Confirm:$false -ErrorAction Ignore

    Write-Information 'DocFX output cleaned. Run "dotnet docfx DocumentationSite/docfx.json" to regenerate.' -InformationAction Continue
}
finally
{
    Pop-Location
}
