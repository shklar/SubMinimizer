#
# GetPackageVersions.ps1
#

function GetInfo
{
   $allPackages = Find-Package -Prerelease -AllVersions
   
   # getting package description as text
   $allDesc = GetPackages $allPackages

   Write-Host 'Getting version data'

   $prjDescDir = Join-Path $PSScriptRoot -ChildPath 'bin\\data'
   if (-not (Test-Path -Path $prjDescDir))
   {
      New-Item -ItemType Directory $prjDescDir
   }

   $allDesc = "Id,Versions`n" + $allDesc
   $allDesc | Out-File -FilePath (Join-Path $prjDescDir -ChildPath 'all_ver.dat')


}

function GetPackages($projPackages)
{
   $txt = ''
   foreach($p in $projPackages)
   {
      $txt += $p.Id 
      $txt += ','
      $txt += $p.Versions + "`n"
   }

   return $txt
}

GetInfo
