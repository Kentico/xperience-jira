using CMS.DocumentEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Kentico.Xperience.Jira.Workflow
{
    /// <summary>
    /// A workflow action which creates a Jira webhook
    /// </summary>
    public class JiraWebhookAction : DocumentWorkflowAction
    {
        public override void Execute()
        {
            var name = GetResolvedParameter("Name", "");
            var events = GetResolvedParameter("Events", "");
            var scope = GetResolvedParameter("Scope", "");

            if (!String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(events))
            {
                var response = JiraHelper.CreateWebhook(Node, name, events, scope);

                var webhook = JObject.Parse(response);
                var uri = new Uri(webhook.Value<string>("self"));
                var id = uri.Segments.Last();
                JiraHelper.LinkJiraWebhook(Node, id);
            }
            else
            {
                throw new NullReferenceException("Webhook name or event was not found in the workflow step configuration.");
            }
        }
    }
}