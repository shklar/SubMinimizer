# this script run at commit time for checking  current content.  if you want check current content earlier just make SecretCheck project current, open this script at visual studio and press F5
# output window show  errors

#
# pre_commit.ps1
#

Write-Host '~Powershell precommit hook. Sorry only commit from Visual Studio supported. Do not commit another way meanwhile'

#  meanwhile check comments readability disabled not to deal with numerous not important changes
$checkSpaceAfterComm =$false

$checkCfgNonNeutralChanges = $true
$checkApplicationInsightsCfg = $true
$checkNotNecessaryChgsCfg = $true
$checkHCUserName = $true
$checkNuGetPackageImportStamp = $true

# meanwhile check application ID and password references and data in code disabled till moving secrets to Azure
$checkSecrets = $false

$checkPublicProfileCfg = $true
$checkBetaVersion = $true

function PreCommit
{

   $err = $false
   
   if ($checkSpaceAfterComm)
   {
      Write-Host "`nCheck comments without space after //`n"
   
      $files = GetFiles('.cs')
      $chFound = CheckComments $files
      if ($chFound)
      {

         Write-Host 'Less readable comments found.'
         $err = $true
      }
   }
   else
   {
      Write-Host "`nCheck comments without space after // skipped`n"
   }

   if ($checkCfgNonNeutralChanges)
   {
      $cfgChFound = $false

      Write-Host "`nCheck from neutral values changes in application configuration`n"

      $files = GetFiles('.config')
   
      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:WebJobDashboardCs\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (WebJobDashboardCs is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }

      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:WebJobStorageCs\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (WebJobStorageCs is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }

      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:ApiKey\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (ApiKey is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }

      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:AppRegId\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (AppRegId is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }

      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:AppRegPassword\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (AppRegPassword is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }
   
      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:KeyVault\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (KeyVault is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }
   
      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:DataAccess\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (DataAccess is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }


      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:EnableWebJob\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (EnableWebJob is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }

      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:\AllowWebJobDelete" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (AllowWebJobDelete is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }

      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:AllowWebJobEmail\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (AllowWebJobEmail is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }

      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:DevTeam\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (DevTeam is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }
   
      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:EnvDisplayName\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (EnvDisplayName is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }
   
      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:ServiceURL\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (ServiceURL is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }

      $chFound = CheckContent -fs $files -pattern 'add key[ ]*=[ ]*\"env:TelemetryInstrumentationKey\" value[ ]*=[ ]*\"([^ ]+)\"' -vals @('Dummy')
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (TelemetryInstrumentationKey is changed from neutral).'
         $cfgChFound = $true
         $err = $true
      }

      if (-not $cfgChFound)
      {
         Write-Host 'No changes at Web.config found changed from neutral.'
      }
   }
   else
   {
      Write-Host "`nCheck from neutral values changes in application configuration skipped`n"
   }

   if ($checkApplicationInsightsCfg)
   {
      Write-Host "`nCheck unnecessary changes in application insights configuration`n"

      $chgs = GetMod 'ApplicationInsights.config'
      $aiModified = $chgs[0]
      $filepath = $chgs[1]

      if ($aiModified)
      {
            Write-Host "`nChanges in application insights configuration discovered`n"
            Write-Host "Please revert changes in ApplicationInsights.config if they are not intentional. If intentional please stage them manually"
            Write-Host "`Otherwise your commit will be forbidden`n"
            $err = $true
      }
      else
      {
            Write-Host "`nNo changes in application insights configuration discovered`n"
      }
   }
   else
   {
      Write-Host "`nCheck unnecessary changes in application insights configuration skipped`n"
   }

   if ($checkNotNecessaryChgsCfg)
   {
      Write-Host "`nCheck unnecessary changes in configuration files`n"

      $files = GetFiles '.config' 'Web'

      $chFound = CheckContent $files 'TelemetryCorrelationHttpModule'
      if ($chFound)
      {
         Write-Host 'Changes at Web.config found (TelemetryCorrelationHttpModule). Are all them intentional?'
         $err = $true
      }
      else
      {
         Write-Host 'Changes at Web.config (TelemetryCorrelationHttpModule) not found.'
      }
   }
   else
   {
      Write-Host "`nCheck unnecessary changes in configuration files skipped`n"
   }


   if ($checkHCUserName)
   {
      Write-Host "`nCheck hard coded user name`n"

      $files = GetFiles('.cs')
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
   }
   else
   {
      Write-Host "`nCheck hard coded user name skipped`n"
   }

   
   if ($checkNuGetPackageImportStamp)
   {
      Write-Host "`nCheck NuGetPackageImportStamp`n"

      $files = GetFiles('.csproj')
      $nugImpFound = CheckContent $files 'NuGetPackageImportStamp'
      if ($nugImpFound)
      {
         Write-Host 'NuGetPackageImportStamp found'
         $err = $true
      }
      else
      {
         Write-Host 'NuGetPackageImportStamp not found'
      }   
   }
   else
   {
      Write-Host "`nCheck NuGetPackageImportStamp skipped`n"
   }

   if ($checkSecrets)
   {
      Write-Host "`nCheck hard coded application ID`n"
   
      $files = GetFiles('.config') 
      $files += GetFiles('.cs')
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
   
      $files = GetFiles('.config') 
      $files += GetFiles('.cs')
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
   }
   else
   {
      Write-Host "`nCheck secrets skipped`n"
   }
   
   if ($chkConnStrings)
   {
      Write-Host "`nCheck hard coded  database connection string`n"
   
      $files = GetFiles('.config')
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
   
      $files = GetFiles('.config')
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

      $files = GetFiles('.config')
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
   }
   

   if ($checkPublicProfileCfg)
   {
      Write-Host "`nCheck  publishing profile reference`n"

      $files = GetFiles('.csproj')
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
   }
   else
   {
      Write-Host "`nCheck  publishing profile reference skipped`n"
   }

   if ($checkBetaVersion)
   {
      Write-Host "`nCheck beta usage`n"
      Write-Host "`nAttention! Using by partial projects different package versions isn't checked meanwhile`n"

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
  }
   else
   {
      Write-Host "`nCheck beta usage skipped`n"
   }
   if ($err -eq $true)
   {
      Write-Host "`n`nErrors found"
      exit 1
   }
   else
   {
      Write-Host "`n`nNo errors found"
   }
}

function GetFiles
{
   param
   (
           [Parameter(Mandatory=$true, Position=0)]
      [string] $ext,
      [Parameter(Mandatory=$false, Position=1)]
      [string] $name
   )

   $curDir = Get-Location

   $fs = Get-ChildItem -path $curDir -recurse | where {$_.extension -eq $ext -and $_.Directory.FullName.IndexOf('obj') -eq -1 -and $_.Directory.FullName.IndexOf('bin') -eq -1 }
   if ($name -ne $null -and $name -ne '')
   {
      return $fs | where {$_.BaseName -eq $name}
   }
   else
   {
      return $fs
   }
}

function GetMod($file)
{
   $chgs = Git status
   $staged = $true
   foreach ($c in $chgs)
   {
      if ($c -match 'not staged')
      {
         $staged = $false
      }

      $cs = $c.ToString()
      if ($cs -match $file)
      {
         if (-not $staged)
         {
            $true
            $m = $cs -match '[^ ]+ ([^ ]+)'
            $matches[1]
            return 
          }
      }
   }

   $false
   $null
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

function CheckComments($fs)
{

   $found = $false

   foreach ($f in $fs)
   {
      $txt = Get-Content -Path $f.FullName
      foreach ($str in $txt)
      {
         $strTxt = $str.ToString()
         if ($strTxt -match '([^/]*)([/]+)[^"^ ^/]')
         {
            $prev = $Matches[1]
            if ($prev -match 'http:$' -or $prev -match 'https:$')
            {
               continue
            }

            $slashes = $matches[2]

            if ($slashes -ne '//')
            {
               continue
            }
          
            Write-Host '-- File --' + $f.FullName
            Write-Host 'Less readable comment found. please add space after slashes'
            Write-Host $strTxt
            $found = $true
         }
      }
   }

   return $found
}

function CheckContent()
{
   param
   (
      [Parameter(Mandatory=$true, Position=0)]
      [Array] $fs,
      [Parameter(Mandatory=$true, Position=1)]
      [string] $pattern,
      [Parameter(Mandatory=$false, Position=2)]
      [Array] $vals
   )

   $found = $false

   foreach ($f in $fs)
   {
      $txt = Get-Content -Path $f.FullName
      foreach ($str in $txt)
      {
         $strTxt = $str.ToString()

         if ($strTxt -match $pattern)
         {
            if ($vals -ne $null)
            {
               $m = $matches[1]
               $foundValidInString = $false
               
               foreach ($v in $vals)
               {
                  $m = $m.ToString()
                  if ($m -eq $v)
                  {
                     $foundValidInString = $true
                  }
               }

               if (-not $foundValidInString)
               {
                   Write-Host '-- File --' + $f.FullName
                   Write-Host $strTxt
                   $found = $true
               }
             }
             else
             {
                  Write-Host '-- File --' + $f.FullName
                  Write-Host $strTxt
                  $found = $true
              }
           }
         }
    }
   
   return $found
}

PreCommit
