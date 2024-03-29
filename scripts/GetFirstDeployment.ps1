[CmdletBinding()]
param([string]$commit)
$mergeDate = get-date (git show -s --format=%ci $commit)
$branches = git branch --remote --contains $commit
$formattedBranches = $branches | ? {$_ -notmatch "->|\*"} | % { $_.replace("  origin/", "refs/heads/")}
$builds = ParallelGet-Builds -BranchNames $formattedBranches -MinTime $mergeDate | % { $_ | ConvertFrom-Json } | % {$_.value}
$releases = ParallelGet-Releases -ArtifactVersionIds $builds.Id -ExpandProperties environments,artifacts  | % { $_ | ConvertFrom-Json } | % {$_.value}
$otherReleases = Get-Release -SourceId $sourceId -MinCreatedTime $mergeDate -ExpandProperties artifacts,environments | ConvertFrom-Json | %{$_.Value}
$buildArtifacts = ParallelGet-BuildArtifacts -BuildIds $builds.id | % { $_ | ConvertFrom-Json} | %{$_.value}
$cloudVaultArtifacts = $buildArtifacts | ? {$_.name -match "CloudVaultArtifact"}
$cloudVaultIds =  $cloudVaultArtifacts.resource.properties | % { "$($_.version) ($($_.definition))" } 
$targetReleases = $otherReleases | ?{ $_.artifacts.definitionReference.version.name -in $cloudVaultIds }
$releases += $targetReleases
$envs = $releases.environments
$activeEnvs = $envs | ? {$_.status -match "succeeded"}
$ByEnvDict = $activeEnvs | % {[PSCustomObject]@{id = $_.id; name = $_.name; queuedOn = (get-date $_.deploySteps.queuedOn); releaseDefinition = $_.releaseDefinition.name} } | ? {$_.queuedOn -ne $null} | % {$byNameDict = @{}} {$byNameDict[$_.Name] += @($_)} {$byNameDict}
$firstDeployments = $ByEnvDict.Keys | % {$firstDeployments = @{}} { $firstDeployments[$_] = $ByEnvDict[$_] | sort -Property queuedOn | select -first 1 } {$firstDeployments}
return $firstDeployments
