function Pretty-Commits
{ 
    [CmdletBinding()]
    param([parameter(ValueFromPipeLine = $true)][string[]]$commits)
    $commits | %{ [pscustomobject]@{ commit = ($_); author = (git show -s --format="%an" $_); message = (git show -s --format="%s" $_); link = "https://msazure.visualstudio.com/One/_git/EngSys-Acis-Legacy/commit/$_" } }
 }
