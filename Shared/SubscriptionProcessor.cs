using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CogsMinimizer.Shared
{
    public static class SubscriptionProcessor
    {
        /// <summary>
        /// The main method triggered periodically. Analyzes each one of the registered subscriptions 
        /// and reports to the customer
        /// If a specific list of subscriptions is provided - only those will be processed. Needed to support the onboarding of a new subscription
        /// </summary>
        public static void ProcessSubscriptions(ITracer tracer, IEnumerable<string> subscriptionsToProcess=null)
        {
            using (var db = new DataAccess())
            {
                tracer.TraceInformation("DB access created");
                var registeredSubscriptions = db.Subscriptions.ToList();

                foreach (var sub in registeredSubscriptions)
                {
                    //if a white list of subscriptions was provided skip any that are not on the list
                    if (subscriptionsToProcess != null && !subscriptionsToProcess.Contains(sub.Id)){
                        continue;
                    }

                    //Analyze the subscription
                    SubscriptionAnalyzer analyzer = new SubscriptionAnalyzer(db, sub, true, tracer);
                    SubscriptionAnalysisResult analysisResult = analyzer.AnalyzeSubscription();
                    sub.LastAnalysisDate = analysisResult.AnalysisStartTime;

                    //Persist analysis results to DB
                    db.SaveChanges();

                    if (ConfigurationManager.AppSettings["env:AllowWebJobEmail"].Equals("True", StringComparison.OrdinalIgnoreCase))
                    {
                        tracer.TraceInformation("AllowWebJobEmail switch is Enabled.");

                        //Send email with the outcome of the analysis
                        EmailSubscriptionAnalysisResult(analysisResult, tracer);
                    }
                    else
                    {
                        tracer.TraceWarning("AllowWebJobEmail switch is disabled. Skipping sending email.");
                    }
                }
            }
        }

        private static void EmailSubscriptionAnalysisResult(SubscriptionAnalysisResult analysisResult, ITracer tracer)
        {
            Subscription sub = analysisResult.AnalyzedSubscription;
            string envDisplayName = ConfigurationManager.AppSettings["env:EnvDisplayName"];
            if (!string.IsNullOrWhiteSpace(envDisplayName))
            {
                envDisplayName = $" [{envDisplayName}]";
            }

            string subject = $"SubMinimizer{envDisplayName}: Subscription Analysis report for {sub.DisplayName}";

            // Don't send mail if all resources are valid if subscription setting set appropriately
            if (sub.SendEmailOnlyInvalidResources)
            {
                if (analysisResult.NotFoundResources.Count == 0 &&
                    analysisResult.NearExpiredResources.Count == 0 &&
                    analysisResult.DeletedResources.Count == 0 &&
                    analysisResult.FailedDeleteResources.Count == 0 &&
                    analysisResult.ExpiredResources.Count == 0)
                {
                    return;
                }
            }

            var message = EmailUtils.CreateEmailMessage(analysisResult, sub);

            var to = new List<Email>();
            var cc = new List<Email>();
            var bcc = new List<Email>();

            //Add To recepients - the subscription admin and anyone with an expired resource 
            to.Add(new Email(sub.ConnectedBy));

            to.AddRange(analysisResult.ExpiredResources.Where(x => !string.IsNullOrWhiteSpace(x.Owner)).Select(x => new Email(x.Owner)));

            //Add CC recepients - the subscription coadmins if so selected by the admin in the settings
            if (sub.SendEmailToCoadmins && analysisResult.Admins != null)
            {
                cc.AddRange(analysisResult.Admins.Select(x => new Email(x)));
            }

            //Add CC recepients - additional recipients if configured by the admin in the settings
            if (!string.IsNullOrWhiteSpace(sub.AdditionalRecipients))
            {
                var additionalRecipients = EmailAddressUtils.splitEmailsString(sub.AdditionalRecipients);
                cc.AddRange(additionalRecipients.Select(x => new Email(x)));
            }

            //Add BCC recepients - dev team, as configured in the app config
            string devTeam = ConfigurationManager.AppSettings["env:DevTeam"];
            if (devTeam != null)
            {
                bcc.AddRange(devTeam.Split(';').Select(x => new Email(x)));
            }

            var email = new SubMinimizerEmail(subject, message, to, cc, bcc);

            EmailUtils.SendEmail(email, tracer).Wait();
        }

    }
}
