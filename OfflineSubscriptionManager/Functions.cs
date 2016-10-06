using System;
using System.Collections.Generic;
using System.Configuration;
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
                                    HTML{background-color: #e8e8e8;}
                                    .courses-table{font-size: 16px; padding: 3px; border-collapse: collapse; border-spacing: 0;}
                                    .courses-table .description{color: #505050;}
                                    .courses-table td{border: 1px solid #D1D1D1; background-color: #F3F3F3; padding: 0 10px;}
                                    .courses-table th{border: 1px solid #424242; color: #FFFFFF;text-align: left; padding: 0 10px;}
                                    .green{background-color: #6B9852;}
                                </style>
                            </head>
                            <body>";

            message += $"<H2>Subscription name : {sub.DisplayName}</H2>";
            message += $"<H2>Subscription ID : {sub.Id} </H2>";
            message += $"<h3>Analysis Date : {GetShortDate(sub.LastAnalysisDate)}</h3>";
            message += "<br>";

            if (analysisResult.ExpiredResources.Count == 0)
            {
                message += "<h3>No expired resources found</h3>";
            }
            else
            {
                message += $"<h3>Found {analysisResult.ExpiredResources.Count} expired resources :</h3>";
                message += GetHTMLTableForResources(analysisResult.ExpiredResources);
            }

            message += "</body></html>";

            SendEmail(subject , message, to).Wait();
        }

        private static string GetHTMLTableForResources(IEnumerable<Resource> resources)
        {
            string result = "<Table class=\"courses-table\">";

            result += "<tr>";
            result += "<th class=\"green\">Name</th>";
            result += "<th class=\"green\">Group</th>";
            result += "<th class=\"green\">Owner</th>";
            result += "<th class=\"green\">Expiration Date</th>";

            result += "</tr>";

            foreach (var resource in resources)
            {
                result += "<tr>";

                result += $"<td>{resource.Name}</td>";
                result += $"<td>{resource.ResourceGroup}</td>";
                string unclearOwner = resource.Owner != null && !resource.ConfirmedOwner ? "(?)" : string.Empty;
                result += $"<td>{resource.Owner} {unclearOwner}</td>";
                result += $"<td>{GetShortDate(resource.ExpirationDate)}</td>";

                result += "</tr>";
            }
            result += "</Table>";

            return result;
        }


        static async Task SendEmail(string subject, string contentMessage, Email to)
        {
            string apiKey = ConfigurationManager.AppSettings["API_KEY"];
            dynamic sg = new SendGridAPIClient(apiKey);

            Email from = new Email("noreply@subminimizer.com");
            
            Content content = new Content("text/html", contentMessage);
            
            Mail mail = new Mail(from, subject, to, content);

            dynamic response = await sg.client.mail.send.post(requestBody: mail.Get());
        }

        private static string GetShortDate(DateTime dateTime)
        {
            return dateTime.ToString("dd MMMM yyyy");
        }
    }
}
