using CMS.Automation;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Xperience.Jira.Automation
{
    /// <summary>
    /// A Marketing automation action which creates a Jira webhook
    /// </summary>
    public class JiraWebhookAction : AutomationAction
    {
        public override void Execute()
        {
            var name = GetResolvedParameter("Name", "");
            var events = GetResolvedParameter("Events", "");
            var scope = GetResolvedParameter("Scope", "");

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(events))
            {
                var response = JiraHelper.CreateWebhook(StateObject, name, events, scope);

                var webhook = JObject.Parse(response);
                var uri = new Uri(webhook.Value<string>("self"));
                var id = uri.Segments.Last();
                JiraHelper.LinkJiraWebhook(StateObject, id);
            }
            else
            {
                throw new NullReferenceException("Webhook name or event was not found in the automation step configuration.");
            }
        }
    }
}