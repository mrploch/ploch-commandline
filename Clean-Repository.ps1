function Remove-Folders( [Parameter(Mandatory=$true)] [string] $Path, [Parameter(Mandatory=$true)] [string[]] $FolderNames, [string[]] $ExcludeWildcards = @() )
{
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
        if ($remove) {
            Write-Output $item.FullName
            Remove-Item -Path $item.FullName -Recurse -Force
        }
    }
}

function Clear-Solution( [Parameter(Mandatory=$true)] [string] $SolutionDirectory, [string[]] $ExcludeWildcards = @() ) {
    Write-Output "solutionDirectory: $SolutionDirectory, excludeWildcards: $ExcludeWildcards"
    if ((Get-Item -Path $SolutionDirectory) -isnot [System.IO.DirectoryInfo]) {
        $SolutionDirectory = [System.IO.Path]::GetDirectoryName($SolutionDirectory)
    }
    Remove-Folders -Path $SolutionDirectory -FolderNames obj,bin,CoverageResults -ExcludeWildcards $ExcludeWildcards
}

Clear-Solution -SolutionDirectory $PSScriptRoot
