using System.Collections.Generic;
using CogsMinimizer.Shared;

namespace CogsMinimizer.Models
{
    /// <summary>
    /// The model used by the Home/Index view and controller
    /// </summary>
    public class HomeIndexViewModel
    {
        public Dictionary<string, Organization> UserOrganizations { get; set; }
        public Dictionary<string, Subscription> UserSubscriptions { get; set; }
        public List<string> UserCanManageAccessForSubscriptions { get; set; }
        public List<string> DisconnectedUserOrganizations { get; set; }
        public List<Resource>  Resources { get; set; }
    }
}