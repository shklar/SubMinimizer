﻿using System;
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
            string htmlLink = $"<a href=\"{url}\">{message}</a>";
            return htmlLink;
        }

        public static string CreateAzureResourceAnchor(string message, string azureResourceId)
        {
            string url = $"https://ms.portal.azure.com/#resource{azureResourceId}";
            string htmlLink = $"<a href=\"{url}\">{message}</a>";
            return htmlLink;
        }


    }
}