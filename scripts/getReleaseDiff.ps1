[CmdletBinding()]
param([int]$fromRelease, [int]$toRelease, [Switch]$force = $false)
. .\Utility.ps1
if(!$force -and $toRelease -le $fromRelease)
{
    Write-Warning "fromRelease should be older than toRelease. If you really mean it, retry with -force." 
    return
}

(git fetch origin) | Out-Null
$fromReleaseMetadata = @{ Id = $fromRelease}
$toReleaseMetadata = @{ Id = $toRelease}

@($fromReleaseMetadata, $toReleaseMetadata) | % { $rel = Convertfrom-Json (Get-Release -ReleaseId $_.Id -ExpandProperties artifacts,environments); $_.Release = $rel } 
@($fromReleaseMetadata, $toReleaseMetadata) | % { $_.sourceId = $_.Release.artifacts.definitionReference.version.name  -replace '.*\(git_engsys_acis_legacy_(.*)\)', 'refs/heads/$1' }

foreach($releaseRecord in @($fromReleaseMetadata, $toReleaseMetadata))
{
    if($releaseRecord.sourceId -match "^\d+$")
    {
        $releaseRecord.sourceBuild = Get-Builds -BuildId $releaseRecord.sourceId | ConvertFrom-Json | ?{$_.result -match "succeeded"}
    }
    else
    {
        $releaseRecord.sourceBuilds = ParallelGet-Builds -BranchNames $releaseRecord.sourceId | Convertfrom-Json | ?{$_.result -match "succeeded"} 
        $releaseRecord.sourceBuild = $releaseRecord.sourceBuilds | sort -property queueTime -Descending | select -First 1
    }
}

$LastCommit = $toReleaseMetadata.sourceBuild.sourceVersion; 
$FirstCommit = $fromReleaseMetadata.sourceBuild.sourceVersion; 

return (git rev-list $LastCommit ^$FirstCommit)

