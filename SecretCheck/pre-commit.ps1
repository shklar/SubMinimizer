#
# pre_commit.ps1
#

Write-Host '~Powershell precommit hook. Sorry only commit from Visual Studio supported. Do not commit another way meanwhile'

function PreCommit
{

   $err = $false
   $curDir = Get-Location
   
   Write-Host "`nCheck hard coded user name`n"

   $files = Get-ChildItem -path $curDir -recurse | where {$_.extension -eq '.cs'} 
   $mailFound = CheckContent $files 'eviten@microsoft.com'
   if ($mailFound)
   {
      Write-Host 'Hard coded user name found'
      $err = $true
   }
   else
   {
      Write-Host 'Hard coded user name not found'
   }

   Write-Host "`nCheck hard coded application ID`n"
   
   $files = Get-ChildItem -path $curDir -recurse | where {($_.extension -eq '.cs') -or ($_.extension -eq '.config')} 
   $appIdFound = CheckContent $files 'ida:ClientID'
   if ($appIdFound)
   {
      Write-Host 'Hard coded app ID found'
      $err = $true
   }
   else
   {
      Write-Host 'Hard coded app ID not found'
   }

   Write-Host "`nCheck hard coded application password`n"
   
   $files = Get-ChildItem -path $curDir -recurse | where {($_.extension -eq '.cs') -or ($_.extension -eq '.config')} 
   $pwdFound = CheckContent $files 'ida:Password'
   if ($pwdFound)
   {
      Write-Host 'Hard coded password found'
      $err = $true
   }
   else
   {
      Write-Host 'Hard coded password not found'
   }

   Write-Host "`nCheck hard coded  database connection string`n"
   
   $files = Get-ChildItem -path $curDir -recurse | where {($_.extension -eq '.config')} 
   $connFound = CheckContent $files 'add name=\"DataAccess'
   if ($connFound)
   {
      Write-Host 'Hard coded connection string found'
      $err = $true
   }
   else
   {
      Write-Host 'Hard coded connection string not found'
   }

   Write-Host "`nCheck hard coded web job connection string`n"
   
   $files = Get-ChildItem -path $curDir -recurse | where {($_.extension -eq '.config')} 
   $connFound1 = CheckContent $files 'WebJobsDash'
   if ($connFound1)
   {
      Write-Host 'Hard coded web job connection string found'
      $err = $true
   }
   else
   {
      Write-Host 'Hard coded web job connection string not found'
   }

   $files = Get-ChildItem -path $curDir -recurse | where {($_.extension -eq '.config')} 
   $connFound2 = CheckContent $files 'WebJobsStorage'
   if ($connFound2)
   {
      Write-Host 'Hard coded web job connection string 2 found'
      $err = $true
   }
   else
   {
      Write-Host 'Hard coded web job connection string 2 not found'
   }

   Write-Host "`nCheck  publishing profile reference`n"

   $files = Get-ChildItem -path $curDir -recurse | where {($_.extension -eq '.csproj')} 
   $pubFound = CheckContent $files 'pubxml'
   if ($pubFound)
   {
      Write-Host 'Publishing profile reference found'
      $err = $true
   }
   else
   {
      Write-Host 'Publishing profile reference not found'
   }

   Write-Host "`nCheck beta usage`n"

   # check version info existence and ask run version info existence script running if info absent
   $verInfo = GetVersionInfo
   if ($verInfo -eq $null)
   {
      Write-Host 'Unfortunately package information is not fully available outside of console manager context.  please run ./SecurityCheck/GetVersion.ps1 from package manager console and repeat commit.'
      $err = $true
   }
   else
   {
      $files = Get-ChildItem -path $curDir -recurse | where {$_.Name -eq 'packages.config' }
      $betaFound = CheckBetaContent $files $verInfo
      if ($betaFound)
      {
         Write-Host 'Beta version usage found'
         $err = $true
      }
      else
      {
         Write-Host 'Beta version usage not found'
      }
   }

   if ($err -eq $true)
   {
      Write-Host 'Errors found'
      exit 1
   }
}

function GetVersionInfo
{
   $verInfoDirectory = Join-Path $PsScriptRoot -childPath "bin\data"
   $verInfoPath = Join-Path $verInfoDirectory -childPath "all_ver.dat"
   Write-Host 'Version Info: ' + $verInfoPath

   if (-not (Test-Path -Path $verInfoDirectory))
   {
      New-Item -ItemType directory -Path $verInfoDirectory
   }

   if (-not (Test-Path -Path $verInfoPath))
   {
      Write-Host 'Version Info is not found: ' + $verInfoPath
      return $null
   }
   else
   {
      Write-Host 'Version Info is found: ' + $verInfoPath
   }

   $minInfoGetTime = (Get-Date).AddDays(-2)
   $verInfoTime = (Get-Item -Path $verInfoPath).LastWriteTime

   if ($verInfoTime -gt $minInfoGetTime)
   {
      $infos = @()
      $infoTxt = Import-Csv $verInfoPath
      
      Write-Host "Version Info loaded"

      foreach($i in $infoTxt)
      
      {
         $id = $i.Id
         $vers = SplitVersions $i.Versions

         $info = New-Object -TypeName psobject 

         $info | Add-Member -MemberType NoteProperty -Name Id -Value $id
         $info | Add-Member -MemberType NoteProperty -Name Vers -Value $vers
         $infos += $info
      }

      return $infos
   }     
   else
   {
      Write-Host "Version Info  exist but outdated"
      return $null
   }

}

function SplitVersions($versions)
{
   $vers = @()
   $from = $versions
   for (;;)
   {
      if ($from -match '[ ]*([^ ]+)')
      {
         $ver = $matches[1]
         $vers += $ver
         $verInd = $from.indexOf($ver)

         $from = $from.Substring($verInd + $ver.length)
      }
      else
      {
         return $vers
      }

   }
}


function CheckBetaContent($fs, $versionInfo)
{
   $found = $false

   # proceed all packages file
   foreach ($f in $files)
   {
      $txt = Get-Content -Path $f.FullName
      # proceed all package descriptions
      foreach ($str in $txt)
      {
         $strTxt = $str.ToString()
         $isPackage = $strTxt -match 'id=\"([^\"]+)\" version=\"([^\"]+)\"'
         if (-not $isPackage)
         {
            continue
         }

         $id = $matches[1]
         $version = $matches[2]

         # preview package found
         if ($version -match 'preview' -or $version -match 'beta'  -or $version -match 'rc')
         {
            # let's get available versions of package and if release available signalize error
            Write-Host $id + '---' + $version

            $idInd = -1
            foreach($ver in $versionInfo)
            {
               if ($ver.Id -eq $id)
               {
                  $idInd = $versionInfo.indexOf($ver)
                  break
               }
            }

            # if package version info  isn't found  there are only preview versions we may use currently selected
            if ($idInd -gt -1)
            {
               # let's check if version array has releases after using currently
               $verArray = $versionInfo[$idInd].Vers
               for ($verInd1 = 0; $verInd1 -le $verArray.length; $verInd1++)
               {
                     if ($verArray[$verInd1] -eq $version)
                     {
                        break
                     }
                }                   

               if ($verInd1 -lt $verArray.length)
               {
                  # currently used version found let's check younger versions if have releases there, if yes signalize error
                  for ($youngVerInd = 0; $youngVerInd -lt $verInd1; $youngVerInd++)
                  {
                     $youngVer = $verArray[$youngVerInd]
                     if ($youngVer -match 'preview')
                     {
                        continue
                     }

                     if ($youngVer -match 'beta')
                     {
                        continue
                     }

                     if ($youngVer -match 'rc')
                     {
                        continue
                     }
                        
                     Write-Host 'Release version is found, use  instead of preview'
                     $found = $true
                        break
                     }

                     if (-not $found)
                     {
                        # younger than used preview package release isn't found probably preview used intentionally. let's check existence of elder releases than use preview and give warning if exist
                        for ($verInd2 = $verInd1 + 1; $verInd2 -lt $verArray.length; $verInd2++)
                        {
                           $elderVersion = $verArray[$verInd2]

                           if ($elderVersion -match 'preview')
                           {
                              continue
                           }


                           if ($elderVersion -match 'beta')
                           {
                              continue
                           }

                           if ($elderVersion -match 'rc')
                           {
                              continue
                           }                        
                        
                           Write-Host 'Warning!!! Release version is found but elder than used release. Was preview used intentionally?'
                        }
                     }
                  }
               }
               else
               {
                  Write-Host 'Release version is not found, package has no release continue using preview'
               } # end of if proceeding package with  versions found
            } #  end of Review package  proceeding
         } # end of cycle by file strings
   } # end of cycle by file

   return $found
}

function CheckContent($fs, $pattern)
{
   $found = $false

   foreach ($f in $files)
   {
      $txt = Get-Content -Path $f.FullName
      foreach ($str in $txt)
      {
         $strTxt = $str.ToString()
         if ($strTxt -match $pattern)
         {
            Write-Host '-- File --' + $f.FullName
            Write-Host $strTxt

            $found = $true
         }
       }
   }

   return $found
}

PreCommit
