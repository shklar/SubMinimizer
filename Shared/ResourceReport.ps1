param(
$sub,
$result);

function ReportResources
{
param(
$sub,
$result);

@'
<!DOCTYPE html>
      <html lang="en">
        <head>
          <meta content="text/html; charset=utf-8" http-equiv="Content-Type" />
          <title>
            Subminimizer report
          </title>
          <style type="text/css">
   html *
{
}
            HTML
   {
      background-color: #ffffff;
   font-family: Segoe UI !important;
      }
            .courses-table{font-size: 16px; padding: 3px; border-collapse: collapse; border-spacing: 0;}
            .courses-table .description{color: #505050;}
            .courses-table td{border: 1px solid #D1D1D1; background-color: #F3F3F3; padding: 0 10px;}
            .courses-table th{border: 1px solid #424242; color: #FFFFFF;text-align: left; padding: 0 10px;}
            .tableheadercolor{background-color: #111111;}
          </style>
        </head>
        <body>
'@
          "<h2><a href='http://subminimizer.azurewebsites.net/Subscription/Analyze/$($sub.Id))'?OrganizationId=$($sub.OrganizationId))&DisplayName=$($sub.DisplayName))'>SubMinimizer report for subscription: $($sub.DisplayName))</a></h2>" 
          "<h2>Subscription ID : $($sub.Id)) </h2>"
          "<h2>Analysis date : $(GetShortDate -Date $sub.LastAnalysisDate) </h2>" 
   "<br/>"
   "<br/>"
   "<br/>"
   "<h2>Legend</h2>"
   GetHTMLSummaryTable -Result $result
   "<br/>"
   "<br/>"
   "<br/>"

          If ($result.DeletedResources.Count -gt 0)
          {
              "<h3>Deleted $($result.DeletedResources.Count) resource(s):</h3>"
              GetHTMLTableForResources -Resources $result.DeletedResources
          }

          If ($result.FailedDeleteResources.Count -gt 0)
          {
              "<h3>Failed deleting $($result.FailedDeleteResources.Count) resource(s):</h3>"
              GetHTMLTableForResources -Resources $result.FailedDeleteResources
          }

          If ($result.ExpiredResources.Count -gt 0)
          {
              "<h3>Found $($result.ExpiredResources.Count) expired resource(s):</h3>"
              If ($sub.ManagementLevel -eq "AutomaticDelete" -or
                    $sub.ManagementLevel -eq "ManualDelete")
                {
                     "<h3><font color=\"#ff0000\"><b>WARNING - Expired resources are about to be deleted!</b></font></h3>" +
                     "<h3>Based on current settings, expired resources will be deleted after $($sub.DeleteIntervalInDays) days </h3>";
                }

              GetHTMLTableForResources -Resources $result.ExpiredResources
          }
          else
          {
               "<h3>No expired resorurces found</h3>"
          }

          If ($result.NotFoundResources.Count -gt 0)
          {
               "<h3>Couldn't find $($result.NotFoundResources.Count) resource(s):</h3>"
               GetHTMLTableForResources($result.NotFoundResources);
          }

           If ($result.NewResources.Count -gt 0)
           {
              "<h3>Found $($result.NewResources.Count) new resource(s) :</h3>"
              GetHTMLTableForResources($result.NewResources);
           }

            If ($result.ValidResources.Count -gt 0)
            {
                "<h3>Found $($result.ValidResources.Count) valid resource(s) :</h3>";
                GetHTMLTableForResources($result.ValidResources);
            }


@'
        </body>
      </html>
'@
}

function GetShortDate
{
   param($Date)
 
   return $Date.ToString("dd-MMM-yy");
}


function GetHTMLSummaryTable
{
   param ($Result);

   
@'
<Table class="courses-table" width="50%">
<tr>
<th class="tableheadercolor">Resource kind</th>
<th class="tableheadercolor">Description</th>
<th class="tableheadercolor">Amount</th>
</tr>
'@

      "<tr>"
      "<td>Deleted</td>"
      "<td>Expired resources that were not prolonged and were deleted since last report automatically or manually.</td>"
      "<td>$($Result.DeletedResources.Count)</td>"
      "</tr>"   
      "<tr>"
      "<td>Failed Deleting</td>"
      "<td>Resources we tried to delete but failed</td>"
      "<td>$($Result.FailedDeleteResources.Count)</td>"
      "</tr>"
      "<tr>"
      "<td>Expired</td>"
      "<td>Resources which expiration  date is less than current.</td>"
      "<td>$($Result.ExpiredResources.Count)</td>"
      "</tr>"
      "<tr>"
      "<td>Not found</td>"
      "<td>Resources expected existing in Azure, but somehow disappeared. Probably deleted manually or we failed to found thems.</td>"
      "<td>$($Result.NotFoundResources.Count)</td>"
      "</tr>"
      "<tr>"
      "<td>New</td>"
      "<td>Resources which were found firstly since last report.</td>"
      "<td>$($Result.NewResources.Count)</td>"
      "</tr>"
      "<tr>"
      "<td>Valid</td>"
      "<td>Resources which expiration date is greater than current.</td>"
      "<td>$($Result.ValidResources.Count)</td>"
      "</tr>"
   "</table>"
}

function GetShortDate
{
   param($Date)
 
   return $Date.ToString("dd-MMM-yy");
}


   function GetHTMLTableForResources
{
   param ($Resources);

   $Resources = $Resources | Sort-Object -Property Type, Name, ExpirationDate
   
@'
<Table class="courses-table">
<tr>
<th class="tableheadercolor">Name</th>
<th class="tableheadercolor">Type</th>
<th class="tableheadercolor">Group</th>
<th class="tableheadercolor">Description</th>
<th class="tableheadercolor">Owner</th>
<th class="tableheadercolor">Expiration Date</th>
</tr>
'@
   ForEach ($res In $Resources)
   {
      "<tr>"
      "<td><a href='https://ms.portal.azure.com/#resource\$($res.AzureResourceIdentifier)'></a>$($res.Name)</td>"
      "<td>$($res.Type)</td>"
      "<td>$($res.ResourceGroup)</td>"
      "<td>$($res.Description)</td>"
      

      If ((-not [string]::IsNullOrWhiteSpace($res.Owner)) -and (-not $res.ConfirmedOwner))
      { 
         $unclearOwner = "(?)";
      } 
      Else 
      { 
         $unclearOwner = string.Empty;
      }

      "<td>$($res.Owner) $($unclearOwner)</td>"
      "<td>$(GetShortDate -Date $res.ExpirationDate)</td>"
      "</tr>"
   }
"</Table>"

}


ReportResources -sub $sub -result $result