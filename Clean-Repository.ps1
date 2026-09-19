<#
.SYNOPSIS
    Deletes the obj, bin and CoverageResults folders under the repository.

.EXAMPLE
    ./Clean-Repository.ps1 -WhatIf

    Lists what would be removed without deleting anything.
#>
[CmdletBinding(SupportsShouldProcess)]
param()

function Remove-BuildFolder
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [Parameter(Mandatory = $true)] [string[]] $FolderNames,
        [string[]] $ExcludeWildcards = @()
    )

    $items = Get-ChildItem -Path $Path -Recurse -Force -Directory -Include $FolderNames
    foreach ($item in $items)
    {
        $remove = $true
        foreach ($excludeWildcard in $ExcludeWildcards) {
            if ($item.FullName -like $excludeWildcard) {
                $remove = $false
                break
            }
        }
        if ($remove -and $PSCmdlet.ShouldProcess($item.FullName, 'Remove folder')) {
            Write-Output $item.FullName
            Remove-Item -Path $item.FullName -Recurse -Force
        }
    }
}

function Clear-Solution
{
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory = $true)] [string] $SolutionDirectory,
        [string[]] $ExcludeWildcards = @()
    )

    Write-Output "solutionDirectory: $SolutionDirectory, excludeWildcards: $ExcludeWildcards"
    if ((Get-Item -Path $SolutionDirectory) -isnot [System.IO.DirectoryInfo]) {
        $SolutionDirectory = [System.IO.Path]::GetDirectoryName($SolutionDirectory)
    }
    Remove-BuildFolder -Path $SolutionDirectory -FolderNames obj,bin,CoverageResults -ExcludeWildcards $ExcludeWildcards
}

Clear-Solution -SolutionDirectory $PSScriptRoot
