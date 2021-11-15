using System.Web.Http;
using CMS;
using CMS.DataEngine;

[assembly: RegisterModule(typeof(Kentico.Xperience.Jira.JiraModule))]

namespace Kentico.Xperience.Jira
{
    /// <summary>
    /// Registers the routes for <see cref="JiraWebhookController"/>
    /// </summary>
    public class JiraModule : Module
    {
        public JiraModule() : base(nameof(JiraModule))
        {
        }

        protected override void OnInit()
        {
            base.OnInit();
            GlobalConfiguration.Configuration.Routes.MapHttpRoute(
                "jiraapi",
                "jiraapi/{type}/{issue}",
                defaults: new { controller = "JiraWebhook", action = "Run", issue = RouteParameter.Optional }
            );
        }
    }
}