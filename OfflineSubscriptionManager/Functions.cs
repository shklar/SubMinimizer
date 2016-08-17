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
using SendGrid;
using SendGrid.Helpers.Mail;

namespace OfflineSubscriptionManager
{
    public class Functions
    {
        // This function will be triggered based on the schedule you have set for this WebJob
        // This function will enqueue a message on an Azure Queue called queue
        [NoAutomaticTrigger]
        public static void ManualTrigger(TextWriter log, int value, [Queue("queue")] out string message)
        {
            log.WriteLine("Function is invoked with value={0}", value);
            message = value.ToString();
            log.WriteLine("Following message will be written on the Queue={0}", message);

            string connString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                
                using (SqlCommand command = new SqlCommand(@"select * from Subscriptions "))
                {
                    command.Connection = conn;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var x = reader.GetString(0);
                        }
                    }
                }
            }

            SendEmail().Wait();

        }

        static async Task SendEmail()
        {
            string apiKey = ConfigurationManager.AppSettings["API_KEY"];
            dynamic sg = new SendGridAPIClient(apiKey);

            Email from = new Email("maximsh@microsoft.com");
            string subject = "Sending with SendGrid is Fun";
            Email to = new Email("maximsh@microsoft.com");
            Content content = new Content("text/plain", "and easy to do anywhere, even with C#");
            content.Value += DateTime.UtcNow.ToShortTimeString();
            Mail mail = new Mail(from, subject, to, content);

            dynamic response = await sg.client.mail.send.post(requestBody: mail.Get());
        }
    }
}
