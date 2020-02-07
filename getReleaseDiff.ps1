[CmdletBinding()]
param([int]$releaseA, [int]$releaseB)

$firstRelease = Get-Release -ReleaseId $releaseB -ExpandProperties artifacts,environments | ConvertFrom-Json
$lastRelease = Get-Release -ReleaseId $releaseA -ExpandProperties artifacts,environments| ConvertFrom-Json
$sourceBranches = @($firstRelease, $lastRelease).artifacts.definitionReference.version.name | % { $_ -replace '.*\(git_engsys_acis_legacy_(.*)\)', 'refs/heads/$1' }
$sourceBuilds = @($sourceBranches[0], $sourceBranches[1]) | ParallelGet-Builds | %{ $_ | ConvertFrom-Json }| % {$_.Value} | ?{$_.result -match "succeeded"} | sort -property queueTime -Descending
$sourceBuilds = @($sourceBranches[0], $sourceBranches[1]) | ParallelGet-Builds | %{ $_ | ConvertFrom-Json }| % {$_.Value} | ?{$_.result -match "succeeded"} | sort -property queueTime -Descending | select -First 2
$LastCommit = $sourceBuilds[0].sourceVersion; $firstCommit = $sourceBuilds[1].sourceVersion
Pretty-Commits (git rev-list $firstCommit..$LastCommit) | fl
