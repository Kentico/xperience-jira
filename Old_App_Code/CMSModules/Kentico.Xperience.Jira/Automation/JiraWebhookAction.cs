using CMS.Automation;
using CMS.EventLog;
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
                throw new InvalidOperationException("Webhook name or event was not found in the automation step configuration.");
            }

            var response = JiraApiHelper.CreateWebhook(StateObject, name, events, scope);
            var content = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                var webhook = JObject.Parse(content);
                var uri = new Uri(webhook.Value<string>("self"));
                var id = uri.Segments.Last();

                JiraHelper.LinkJiraWebhook(StateObject, id);
            }
            else
            {
                LogMessage(EventType.ERROR, nameof(Execute), content, StateObject);
            }
        }
    }
}