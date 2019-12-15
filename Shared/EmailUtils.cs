using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;

namespace CogsMinimizer.Shared
{
    class EmailUtils
    {
        internal static string CreateEmailMessage(SubscriptionAnalysisResult analysisResult, Subscription sub)
        {
            Diagnostics.EnsureArgumentNotNull(() => sub);
            Diagnostics.EnsureArgumentNotNull(() => analysisResult);

            string message = @"<!DOCTYPE html>
                            <html lang=""en"">
                            <head>    
                                <meta content=""text/html; charset=utf-8"" http-equiv=""Content-Type"">
                                <title>
                                   Subminimizer report
                                </title>
                                <style type=""text/css"">
                                    HTML{background-color: #ffffff;}
                                    .courses-table{font-size: 16px; padding: 3px; border-collapse: collapse; border-spacing: 0;}
                                    .courses-table .description{color: #505050;}
                                    .courses-table td{border: 1px solid #D1D1D1; background-color: #F3F3F3; padding: 0 10px;}
                                    .courses-table th{border: 1px solid #424242; color: #FFFFFF;text-align: left; padding: 0 10px;}
                                    .tableheadercolor{background-color: #111111;}
                                </style>
                            </head>
                            <body>";

            string serviceURL = ConfigurationManager.AppSettings["env:ServiceURL"]; ;
            string analyzeControllerLink = $"{serviceURL}/Subscription/Analyze/";
            string headerLink = HTMLUtilities.CreateHTMLLink($"SubMinimizer report for subscription: {sub.DisplayName}",
                $"{analyzeControllerLink}/{sub.Id}?OrganizationId={sub.OrganizationId}&DisplayName={sub.DisplayName}");

            message += $"<H2>{headerLink}</H2>";

            message += $"<H2>Subscription ID : {sub.Id} </H2>";
            message += $"<H2>Analysis Date : {GetShortDate(sub.LastAnalysisDate)}</H2>";
            message += "<br>";

            if (analysisResult.DeletedResources.Count != 0)
            {
                message += $"<h3>Deleted {analysisResult.DeletedResources.Count} resource(s):</h3>";
                message += GetHTMLTableForResources(analysisResult.DeletedResources);
            }


            if (analysisResult.FailedDeleteResources.Count != 0)
            {
                message += $"<h3>Failed deleting {analysisResult.FailedDeleteResources.Count} resource(s):</h3>";
                message += GetHTMLTableForResources(analysisResult.FailedDeleteResources);
            }

            if (analysisResult.ExpiredResources.Count == 0)
            {
                message += "<h3>No expired resources found</h3>";
            }
            else
            {
                message += $"<h3>Found {analysisResult.ExpiredResources.Count} expired resource(s):</h3>";
                if (sub.ManagementLevel == SubscriptionManagementLevel.AutomaticDelete ||
                    sub.ManagementLevel == SubscriptionManagementLevel.ManualDelete)
                {
                    message +=
                        "<h3><font color=\"#ff0000\"><b>WARNING - Expired resources are about to be deleted!</b></font></h3>" +
                        $"<h3>Based on current settings, expired resources will be deleted after {sub.DeleteIntervalInDays} days </h3>";
                }

                message += GetHTMLTableForResources(analysisResult.ExpiredResources);
            }

            if (analysisResult.NotFoundResources.Count != 0)
            {
                message += $"<h3>Couldn't find {analysisResult.NotFoundResources.Count} resource(s):</h3>";
                message += GetHTMLTableForResources(analysisResult.NotFoundResources);
            }

            if (analysisResult.NewResources.Count != 0)
            {
                message += $"<h3>Found {analysisResult.NewResources.Count} new resource(s) :</h3>";
                message += GetHTMLTableForResources(analysisResult.NewResources);
            }

            if (analysisResult.ValidResources.Count != 0)
            {
                message += $"<h3>Found {analysisResult.ValidResources.Count} valid resource(s) :</h3>";
                message += GetHTMLTableForResources(analysisResult.ValidResources);
            }

            message += "</body></html>";
            return message;
        }

        private static string GetHTMLTableForResources(IEnumerable<Resource> resources)
        {
            resources = resources.OrderBy(x => x.ExpirationDate);

            string result = "<Table class=\"courses-table\">";

            result += "<tr>";
            result += "<th class=\"tableheadercolor\">Name</th>";
            result += "<th class=\"tableheadercolor\">Type</th>";
            result += "<th class=\"tableheadercolor\">Group</th>";
            result += "<th class=\"tableheadercolor\">Description</th>";
            result += "<th class=\"tableheadercolor\">Owner</th>";
            result += "<th class=\"tableheadercolor\">Expiration Date</th>";

            result += "</tr>";

            foreach (var resource in resources)
            {
                result += "<tr>";

                result += $"<td><a href=\"https://ms.portal.azure.com/#resource{resource.AzureResourceIdentifier}\">{resource.Name}</a></td>";
                //result += $"<td>{CreateHTMLLink(resource.Name, "https://ms.portal.azure.com/#resource\{resource.AzureResourceIdentifier}\\")}</td>";
                result += $"<td>{resource.Type}</td>";
                result += $"<td>{resource.ResourceGroup}</td>";
                result += $"<td>{resource.Description}</td>";
                string unclearOwner = !string.IsNullOrWhiteSpace(resource.Owner) && ! resource.ConfirmedOwner ? "(?)" : string.Empty;
                result += $"<td>{resource.Owner} {unclearOwner}</td>";
                result += $"<td>{GetShortDate(resource.ExpirationDate)}</td>";

                result += "</tr>";
            }
            result += "</Table>";

            return result;
        }
        private static string GetShortDate(DateTime dateTime)
        {
            return dateTime.ToString("dd-MMM-yy");
        }


        public static async Task SendEmail(SubMinimizerEmail email, ITracer tracer)
        {
            Diagnostics.EnsureArgumentNotNull(() => email);
            Diagnostics.EnsureArgumentNotNull(() => tracer);

            string apiKey = ConfigurationManager.AppSettings["API_KEY"];
            tracer.TraceInformation($"API_KEY length: {apiKey.Length}");

            dynamic sg = new SendGrid.SendGridAPIClient(apiKey);

            Email from = new Email("noreply@subminimizer.com");

            Content content = new Content("text/html", email.Content);

            email.NormalizeAddresses();
            
            Mail mail = new Mail( from, email.Subject, email.To.First(), content);

            mail.Personalization[0].Tos = email.To.ToList();
            if (email.CC.Any())
            {
                mail.Personalization[0].Ccs = email.CC.ToList();
            }

            if (email.BCC.Any())
            {
                mail.Personalization[0].Bccs = email.BCC.ToList();
            }

            tracer.TraceInformation($"Sending email To: { string.Join(";", mail.Personalization[0].Tos.Select(item=>item.Address))} " +
                $"Subject: {email.Subject} " +
                $"Cc: { string.Join(";", email.CC.Select(item=> item.Address))} " +
                                    $"Bcc: { string.Join(";", email.BCC.Select(item => item.Address))}");

            dynamic response = await sg.client.mail.send.post(requestBody: mail.Get());
            tracer.TraceInformation($"Email was sent for {email.Subject}");
            tracer.TraceInformation($"SendGrid response: {response.StatusCode}");
        }
    }
}
