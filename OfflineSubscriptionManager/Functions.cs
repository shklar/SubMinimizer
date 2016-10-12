using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Mapping;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Net;
using CogsMinimizer.Shared;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace OfflineSubscriptionManager
{
    public class Functions
    {
        // This function will be triggered based on the schedule you have set for this WebJob
        [NoAutomaticTrigger]
        public static void ManualTrigger(TextWriter log)
        {
            ProcessSubscriptions();
        }

        /// <summary>
        /// The main method triggered periodically. Analyzes each one of the registered subscriptions 
        /// and reports to the customer
        /// </summary>
        private static void ProcessSubscriptions()
        {
            using (var db = new DataAccess())
            {
                foreach (var sub in db.Subscriptions.ToList())
                {
                    //Analyze the subscription
                    SubscriptionAnalyzer analyzer = new SubscriptionAnalyzer(db, sub, true);
                    SubscriptionAnalysisResult analysisResult = analyzer.AnalyzeSubscription();
                    sub.LastAnalysisDate = analysisResult.AnalysisStartTime.Date;
                    
                    //Persist analysis results to DB
                    db.SaveChanges();

                    //Report the outcome of the analysis
                    ReportSubscriptionAnalysisResult(analysisResult);
                }
            }
            // SendEmail(message).Wait();
        }

        private static void ReportSubscriptionAnalysisResult(SubscriptionAnalysisResult analysisResult)
        {
            //Email to = new Email("maximsh@microsoft.com");
            Subscription sub = analysisResult.AnalyzedSubscription;
            Email to = new Email(sub.ConnectedBy);
            string subject = $"SubMinimizer: Subscription Analysis report for {sub.DisplayName}";

            string message = @"<!DOCTYPE html>
                            <html lang=""en"">
                            <head>    
                                <meta content=""text/html; charset=utf-8"" http-equiv=""Content-Type"">
                                <title>
                                    Upcoming topics
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

         
            string analyzeControllerLink = "http://subminimizer.azurewebsites.net/Subscription/Analyze/";
            string headerLink = CreateHTMLLink($"Subminimizer report for subscription: {sub.DisplayName}",
                $"{analyzeControllerLink}/{sub.Id}?OrganizationId={sub.OrganizationId}&DisplayName={sub.DisplayName}");

            message += $"<H2>{headerLink}</H2>";

            message += $"<H2>Subscription ID : {sub.Id} </H2>";
            message += $"<h3>Analysis Date : {GetShortDate(sub.LastAnalysisDate)}</h3>";
            message += "<br>";

            if (analysisResult.ExpiredResources.Count == 0)
            {
                message += "<h3>No expired resources found</h3>";
            }
            else
            {
                message += $"<h3>Found {analysisResult.ExpiredResources.Count} expired resource(s):</h3>";
                message += GetHTMLTableForResources(analysisResult.ExpiredResources);
            }

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

            message += "</body></html>";

            SendEmail(subject , message, to).Wait();
        }

        private static string GetHTMLTableForResources(IEnumerable<Resource> resources)
        {
            string result = "<Table class=\"courses-table\">";

            result += "<tr>";
            result += "<th class=\"tableheadercolor\">Name</th>";
            result += "<th class=\"tableheadercolor\">Group</th>";
            result += "<th class=\"tableheadercolor\">Owner</th>";
            result += "<th class=\"tableheadercolor\">Expiration Date</th>";

            result += "</tr>";

            foreach (var resource in resources)
            {
                result += "<tr>";

                result += $"<td><a href=\"https://ms.portal.azure.com/#resource{resource.AzureResourceIdentifier}\">{resource.Name}</a></td>";
                //result += $"<td>{CreateHTMLLink(resource.Name, "https://ms.portal.azure.com/#resource\{resource.AzureResourceIdentifier}\\")}</td>";
                result += $"<td>{resource.ResourceGroup}</td>";
                string unclearOwner = resource.Owner != null && !resource.ConfirmedOwner ? "(?)" : string.Empty;
                result += $"<td>{resource.Owner} {unclearOwner}</td>";
                result += $"<td>{GetShortDate(resource.ExpirationDate)}</td>";

                result += "</tr>";
            }
            result += "</Table>";

            return result;
        }

        private static string CreateHTMLLink(string message, string url)
        {
            string htmlLink = $"<a href=\"{url}\">{message}</a>";
            return htmlLink;
        }

        static async Task SendEmail(string subject, string contentMessage, Email to)
        {
            string apiKey = ConfigurationManager.AppSettings["API_KEY"];
            dynamic sg = new SendGridAPIClient(apiKey);

            Email from = new Email("noreply@subminimizer.com");
            
            Content content = new Content("text/html", contentMessage);
            
            Mail mail = new Mail(from, subject, to, content);
            var bccList = new List<Email>();
            var dev1Email = new Email("maximsh@microsoft.com");
            if (!to.Address.Equals(dev1Email.Address))
            {
                bccList.Add(dev1Email);
            }
            var dev2Email = new Email("eviten@microsoft.com");
            if (!to.Address.Equals(dev2Email.Address))
            {
                bccList.Add(dev2Email);
            }

            mail.Personalization[0].Bccs = bccList;

           dynamic response = await sg.client.mail.send.post(requestBody: mail.Get());
        }

        private static string GetShortDate(DateTime dateTime)
        {
            return dateTime.ToString("dd MMMM yyyy");
        }

        
    }
}
