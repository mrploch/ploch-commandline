<#
.SYNOPSIS
    Removes the generated DocFX output so the next build starts from a clean state.

.DESCRIPTION
    Deletes the rendered site and the API metadata DocFX regenerates from the
    Ploch.CommandLine.Spectre projects. api/toc.yml is authored by hand and is kept.

    Run this when renamed or removed public types leave stale pages behind: DocFX
    overwrites the metadata it regenerates but does not delete files whose source
    symbol no longer exists.

.EXAMPLE
    ./Clean-DocFx-Common.ps1 -WhatIf

    Lists what would be removed without deleting anything.
#>
[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'

Push-Location $PSScriptRoot
try
{
    # A path that is simply absent is the expected state on a fresh clone, so it is skipped rather
    # than ignored. Everything else -- a locked file, a permission failure, a partial recursive
    # delete -- terminates, because reporting a clean state while stale pages survive would let the
    # next DocFX build reuse them.
    if (Test-Path -Path '_site')
    {
        Remove-Item -Path '_site' -Recurse -Force
    }

    if (Test-Path -Path 'api')
    {
        # toc.yml is hand-authored; everything else under api/ is DocFX metadata output. The glob is
        # resolved inside the guard because PowerShell expands it before Remove-Item can react to a
        # missing directory.
        Remove-Item -Path 'api/*.yml' -Exclude 'toc.yml' -Force

        if (Test-Path -Path 'api/.manifest')
        {
            Remove-Item -Path 'api/.manifest' -Force
        }
    }

    Write-Information 'DocFX output cleaned. Run "dotnet docfx DocumentationSite/docfx.json" to regenerate.' -InformationAction Continue
}
finally
{
    Pop-Location
}
