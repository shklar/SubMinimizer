# this script must be run manually at any folder containing repository for check
# change lines for appropriate folder getting and branch for check
$branch_name = 'mov_secr_az'
cd ('C:\projects\Sub_prd')

GetCommitsToProceed

function GetCommitsToProceed()
{

   $branch = Git branch -v
   Write-Host 'Current branch is:' 
   $branch

   # getting commits to all branch after current branch creating from master
   $revToSearch = GetSearchCommitsAll($branch_name)

   Write-Host '---Commits---'
   $revToSearch

   Write-Host '---Searching mail---'
   Git grep  -e 'eviten@microsoft'  $revToSearch
   
   Write-Host '---Searching app id---'
   Git grep -e  'ida:ClientID'  $revToSearch
 
   Write-Host '---Searching password---'
   Git grep -e  'ida:Password'  $revToSearch

   Write-Host '---Searching data connection string---'
   Git grep -e  '<add name=[^ ]*DataAccess'  $revToSearch

   Write-Host '---Searching web job data connection string---'
   Git grep -e  'WebJobsDash'  $revToSearch

   Write-Host '---Searching web job data connection string 2---'
   Git grep -e  'WebJobs'  $revToSearch

   Write-Host '---Searching beta packages---'
   Git grep -e  'preview'  $revToSearch
}

function GetSearchCommits($branch)
{
   # return or first common or 10th commit
   $branchHistory = Git --no-pager log  $branch
   $masterHistory = Git --no-pager log  'master'
  
   $revsToSearch = @()
   for($revNum = 0; $revNum -lt $branchHistory.Length; $revNum++)
   {
      $revMark = $branchHistory[$revNum].ToString()
      if (-not $revMark.StartsWith("commit "))
      {
          continue
      }

      if ($branch -eq 'master')
      {
         $m = $revMark -match 'commit ([^ ]+)'
         $commitId =  $matches[1]
        
         $revsToSearch += $commitId
         continue
      }

      for($masterRevNum = 0; $masterRevNum -lt $masterHistory.Length; $masterRevNum++)
      {
         $masterRevMark = $masterHistory[$masterRevNum].ToString()

         if (-not $masterRevMark.StartsWith("commit "))
         {
             continue
         }
         
         if ($masterRevMark -eq $revMark)
         {
            return $revsToSearch
         }
      }

      $m = $revMark -match 'commit ([^ ]+)'
      $commitId =  $matches[1]
        
      $revsToSearch += $commitId
   }
      return $revsToSearch        
 }

function SplitCommits($commit)
{
   $children = @()
   $from = $commit
   for (;;)
   {
      if ($from -match '[ ]*([^ ]+)')
      {
         $child = $matches[1]
         $children += $child
         $from = $from -replace $child, ''
      }
      else
      {
         return $children
      }

   }
}

function GetCommitParentCommits($c)
{
   if ($c.indexOf(' ') -ne -1)
   {
      Write-Host "Wrong commit ID"
   }

   $parent = Git show --pretty=%P $c
   $parentId = $parent[0].ToString()

   if ($parentId -eq '')
   {
      return $null
   }
   
   $allParents = @()
   
   if ($parentId.indexOf(' ') -ne -1)
   {
      $pars = SplitCommits($parentId)
      foreach($p in $pars)
      {
         # sometimes commit itself included into parents don't call get parents again for same commit
         if ($p -eq $c)
         {
            continue
         }

         $parents = GetCommitParentCommits($p)
         if ($parents -ne $null)
         {
            $allParents += $parents
         }         
      }
   }
   else
   {
      $allParents += $parentId

      $parents = GetCommitParentCommits($parentId)
      if ($parents -ne $null)
      {
         $allParents += $parents
      }
   }

   return $allParents
}

function GetParentCommits($commits)
{
   $allParents = @()
   foreach ($c in $commits)
   {
      $p = GetCommitParentCommits($c)
      if ($p -ne $null)
      {
         $allParents += $p
      }
   }

   return $allParents
}

function GetChildrenCommits($commits)
{
   $allChildren = @()
   foreach ($c in $commits)
   {
      $children = Git rev-list --children $c
      foreach ($child in $children)
      {
         $childString = $child.ToString()
         if ($childString.indexOf(" ") -ne -1)
         {
            $spl = SplitCommits($childString)
            $allChildren += $spl
         }
         else
         {
            $allChildren += $childString
         }
      }
   }

   return $allChildren
}

function GetSearchCommitsAll($branch)
{
   # get commits without attention to  parent information

   # meanwhile very rough variant
   
   # this one has no last active development sequence commits
   $branchHistory = Git --no-pager log  $branch

   $revsToSearch = @()
   for($revNum = 0; $revNum -lt $branchHistory.Length; $revNum++)
   {
      $revMark = $branchHistory[$revNum].ToString()
      if (-not $revMark.StartsWith('commit '))
      {
          continue
      }

      $m = $revMark -match 'commit ([^ ]+)'
      $commitId =  $matches[1]
        
      $revsToSearch += $commitId
   }

   # this returns all commits excluding he detached heads last child
   $revList = Git rev-list --all
   $revsToSearch += $revList


   # seems that those listed above is enough
   # but  meanwhile we'll try get children of commits collected
   # lost commits which are detached head last commits and their parents which are  detached head content
   $children = GetChildrenCommits($revsToSearch)
   $revsToSearch += $children

   # meanwhile search children commits doesn't find multi fork children. complicated commits trees aren't expected but collect  lost commits from  detached changes sequences
   $lost = GetLostCommits
   $lostParents = GetParentCommits($lost)
   $revsToSearch += $lost
   $revsToSearch += $lostParents
   
   return RemoveDuplicates($revsToSearch)
 }

function GetLostCommits()
{
     $lost = Git fsck --full --lost-found --unreachable --dangling 
      $lost = $lost -match '[\s\S]+commit ([^ ])+'     
      $lost = $lost.ForEach({
         $m = $_ -match 'unreachable commit ([^ ]+)' 
         $matches[1]
      })  
   
   return $lost
}

function RemoveDuplicates($commits)
{
   $uniqueCommits = @()

   foreach ($c in $commits)
   {
      $exist = $false
      foreach($uc in $uniqueCommits)
      {
         if ($uc.ToString() -eq  $c.ToString())
         {
             $exist = $true
            break
         }
         
      }
      
      if ($exist -ne $true)
      {
         $uniqueCommits += $c
      }
      
   }

   return $uniqueCommits
}

 
