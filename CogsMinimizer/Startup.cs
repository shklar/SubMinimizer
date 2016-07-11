using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(CogsMinimizer.Startup))]
namespace CogsMinimizer
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
