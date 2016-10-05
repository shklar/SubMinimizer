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
                            <html>
                            <head>
                            <style>
                            table, th, td {
                                border: 1px solid black;
                                border-collapse: collapse;
                            }
                            </style>
                            </head>
                            <body>";

            message += $"<H2>Subscription name : {sub.DisplayName}</H2>";
            message += $"<H2>Subscription ID : {sub.Id} </H2>";
            message += $"<h3>Analysis Date : {sub.LastAnalysisDate}</h3>";
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
            string result = "<Table>";
            foreach (var resource in resources)
            {
                result += "<tr>";

                result += $"<td>{resource.Name}</td>";
                result += $"<td>{resource.ResourceGroup}</td>";

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
    }
}
