function Get-FolderName( [Parameter(Mandatory=$true)] [string] $path ) {
    if ((Get-Item -Path $solutionDirectory) -is [System.IO.DirectoryInfo]) {
        return $path
    }
    return [System.IO.Path]::GetDirectoryName($solutionDirectory)
}

function Clear-Solution( [Parameter(Mandatory=$true)] [string] $solutionDirectory, [string[]] $excludeWildcards = @() ) {
    $solutionDirectory = Get-FolderName -path $solutionDirectory    
    Remove-Folders -path $solutionDirectory -folderNames obj,bin,CoverageResults -excludeWildcards $excludeWildcards
}

function Remove-Folders( [Parameter(Mandatory=$true)] [string] $path, [Parameter(Mandatory=$true)] [string[]] $folderNames, [string[]] $excludeWildcards = @())
{   
    $items = get-childitem -Path $path -Recurse -force -Directory -Include $folderNames
    foreach($item in $items)
    {
        $remove = $true
        foreach ($excludeWildcard in $excludeWildcards) {
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

function Clear-Solution( [Parameter(Mandatory=$true)] [string] $solutionDirectory, [string[]] $excludeWildcards = @() ) {
	Write-Output "solutionDirectory: $solutionDirectory, excludeWildcards: $excludeWildcards"
    if ((Get-Item -Path $solutionDirectory) -isnot [System.IO.DirectoryInfo]) {
        $solutionDirectory = [System.IO.Path]::GetDirectoryName($solutionDirectory)
    }
    Remove-Folders -path $solutionDirectory -folderNames obj,bin,CoverageResults -excludeWildcards $excludeWildcards
}

Clear-Solution -solutionDirectory $PSScriptRoot -excludeWildcards "*\data-mgmt-agent\Install\Installers\*"

