using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CogsMinimizer.Shared
{
    public static class HTMLUtilities
    {
        public static string CreateHTMLLink(string message, string url)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => message);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => url);

            string htmlLink = $"<a href=\"{url}\">{message}</a>";
            return htmlLink;
        }

        public static string CreateAzureResourceAnchor(string message, string azureResourceId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => message);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => azureResourceId);

            string url = $"https://ms.portal.azure.com/#resource{azureResourceId}";
            string htmlLink = CreateHTMLLink(message, url);
            return htmlLink;
        }


    }
}
