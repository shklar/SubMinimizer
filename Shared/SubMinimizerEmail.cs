using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;

namespace CogsMinimizer.Shared
{
    class SubMinimizerEmail
    {
        public string Subject { get; set; }
        public string Content { get; set; }
        public IEnumerable<Email> To { get; private set; }
        public IEnumerable<Email> CC { get; private set; }
        public IEnumerable<Email> BCC { get; private set; }

        public SubMinimizerEmail(string subject, string content, IEnumerable<Email> to, IEnumerable<Email> cc, IEnumerable<Email> bcc)
        {
            Subject = subject;
            Content = content;
            To = to;
            CC = cc;
            BCC = bcc;
        }

        /// <summary>
        /// Makes sure every one of the addresses appears only once and in the highest level
        /// To->Cc->Bcc
        /// </summary>
        public void NormalizeAddresses()
        {
            To = To.Select(x=>x.Address).Distinct().Select(x=> new Email(x));
            var newCC= new List<Email>();
            var newBCC = new List<Email>();

            var handledEmails = To.Select(email => email.Address).ToList();

            foreach (var email in CC)
            {
                if (!handledEmails.Contains(email.Address))
                {
                    newCC.Add(email);
                    handledEmails.Add(email.Address);
                }   
            }

            foreach (var email in BCC)
            {
                if (!handledEmails.Contains(email.Address))
                {
                    newBCC.Add(email);
                    handledEmails.Add(email.Address);
                }
            }

            CC = newCC;
            BCC = newBCC;

        }
    }
}
