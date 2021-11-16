using CMS.Automation;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Kentico.Xperience.Jira.Automation
{
    /// <summary>
    /// A Marketing automation action which creates a Jira webhook.
    /// </summary>
    public class JiraWebhookAction : AutomationAction
    {
        public override void Execute()
        {
            var name = GetResolvedParameter("Name", "");
            var events = GetResolvedParameter("Events", "");
            var scope = GetResolvedParameter("Scope", "");

            if (String.IsNullOrEmpty(name) || String.IsNullOrEmpty(events))
            {
                throw new NullReferenceException("Webhook name or event was not found in the automation step configuration.");
            }

            var response = JiraApiHelper.CreateWebhook(StateObject, name, events, scope);

            var webhook = JObject.Parse(response);
            var uri = new Uri(webhook.Value<string>("self"));
            var id = uri.Segments.Last();

            JiraHelper.LinkJiraWebhook(StateObject, id);
        }
    }
}