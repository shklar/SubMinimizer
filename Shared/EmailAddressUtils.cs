using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CogsMinimizer.Shared
{
    //Handeles validations and manipulations of email addresses
    public static class EmailAddressUtils
    {

        public static string ValidateAndNormalizeEmails(string emails)
        {
            if (string.IsNullOrWhiteSpace(emails))
            {
                return null;
            }

            return string.Join(";", NormailzeEmails(emails, true));
        }

        public static string[] splitEmailsString(string emails)
        {
            return emails.Split(new char[] { ';' });
        }

        /// <summary>
        /// Breaks down an emails list string
        /// </summary>
        /// <param name="input">list of emails separated by ; or , </param>
        /// <param name="shouldValidate">Should we check this is a valid email</param>
        /// <returns></returns>
        public static List<string> NormailzeEmails(string input, bool shouldValidate)
        {
            var normalizedAddresses = new List<string>();
            var listofMails = input.Trim().Split(new char[] { ';', ',' });

            if (shouldValidate)
            {
                foreach (var emailAddress in listofMails)
                {
                    if (IsValidEmail(emailAddress))
                    {
                        normalizedAddresses.Add(emailAddress);
                    }
                }
            }
            return normalizedAddresses;
        }

        /// <summary>
        /// Copied shamelessly from 
        /// https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    var domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                return false;
            }
            catch (ArgumentException e)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }

}
