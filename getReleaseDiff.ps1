[CmdletBinding()]
param([int]$fromRelease, [int]$toRelease)

(git fetch origin) | Out-Null
$fromReleaseMetadata = @{ Id = $fromRelease}
$toReleaseMetadata = @{ Id = $toRelease}

@($fromReleaseMetadata, $toReleaseMetadata) | %{ $rel = Convertfrom-Json (Get-Release -ReleaseId $_.Id -ExpandProperties artifacts,environments); $_.Release = $rel } 

#$fromReleaseMetadata.Release = Get-Release -ReleaseId $fromRelease -ExpandProperties artifacts,environments| ConvertFrom-Json
#$toReleaseMetadata.Release = Get-Release -ReleaseId $toRelease -ExpandProperties artifacts,environments | ConvertFrom-Json

@($fromReleaseMetadata, $toReleaseMetadata) | % { $_.sourceId = $_.Release.artifacts.definitionReference.version.name  -replace '.*\(git_engsys_acis_legacy_(.*)\)', 'refs/heads/$1' }


foreach($releaseRecord in @($fromReleaseMetadata, $toReleaseMetadata))
{
    if($identifier -match "^\d+$")
    {
        $releaseRecord.sourceBuild = Get-Builds -BuildId $releaseRecord.sourceId | ConvertFrom-Json | ?{$_.result -match "succeeded"}
    }
    else
    {
        $releaseRecord.sourceBuilds = ParallelGet-Builds -BranchName $releaseRecord.sourceId | Convertfrom-Json | ?{$_.result -match "succeeded"} 
        $releaseRecord.sourceBuild = $releaseRecord.sourceBuilds | sort -property queueTime -Descending | select -First 1
    }
}

#$branchSourceBuilds = $sourceBranches | ParallelGet-Builds | %{ $_ | ConvertFrom-Json } | % {$_.Value} | ?{$_.result -match "succeeded"} 
#$idSourceBuilds = $sourceIds | % { Get-Builds -BuildId $_ } | ConvertFrom-Json | ?{$_.result -match "succeeded"} 

$LastCommit = $toReleaseMetadata.sourceBuild.sourceVersion; 
$FirstCommit = $fromReleaseMetadata.sourceBuild.sourceVersion; 
#Pretty-Commits (git rev-list $FirstCommit..$LastCommit) | fl
return (git rev-list $LastCommit ^$FirstCommit)

