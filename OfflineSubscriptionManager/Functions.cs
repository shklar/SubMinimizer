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
        // This function will enqueue a message on an Azure Queue called queue
        [NoAutomaticTrigger]
        public static void ManualTrigger(TextWriter log)
        {
            log.WriteLine("Function is invoked with value");
     
            string message = "Expired resources report:" + Environment.NewLine;

            using (var db = new DataAccess())
            {
                foreach (var sub in db.Subscriptions)
                {
                    SubscriptionAnalysis analysis = new SubscriptionAnalysis(db, sub);
                    SubscriptionAnalysisResult analysisResult = analysis.AnalyzeSubscription();
                } 
            }

            SendEmail(message).Wait();

        }

  


        static async Task SendEmail(string contentMessage)
        {
            string apiKey = ConfigurationManager.AppSettings["API_KEY"];
            dynamic sg = new SendGridAPIClient(apiKey);

            Email from = new Email("maximsh@subminimizer.com");
            string subject = "Sending with SendGrid is Fun";
            Email to = new Email("maximsh@microsoft.com");
            Content content = new Content("text/plain", contentMessage);
            content.Value += DateTime.UtcNow.ToShortTimeString();
            Mail mail = new Mail(from, subject, to, content);

            dynamic response = await sg.client.mail.send.post(requestBody: mail.Get());
        }
    }
}
