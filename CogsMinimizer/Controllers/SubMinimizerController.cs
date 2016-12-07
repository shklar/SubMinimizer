using CogsMinimizer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IdentityModel.Claims;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CogsMinimizer.Shared;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Management.Authorization.Models;
using Resource = CogsMinimizer.Shared.Resource;

namespace CogsMinimizer.Controllers
{
    public class SubMinimizerController : Controller
    {

        protected override void OnException(ExceptionContext context)
        {
            context.ExceptionHandled = true;
            context.HttpContext.Response.Clear();
            Response.StatusCode = 500;

            Exception excep = context.Exception;
            var stack = excep.StackTrace ?? string.Empty;
            var text = $"Exception happened. Message: {excep.Message} Stack: {stack}";

            System.Diagnostics.Trace.TraceError(text);
     
            context.Result = RedirectToAction("Error", "Home", new { Exception = context.Exception.Message});
        }
    }
}