using CMS.Base;
using CMS.DataEngine;
using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.Jira.Events
{
    /// <summary>
    /// Jira handler.
    /// </summary>
    public class JiraWebhookHandler : AdvancedHandler<JiraWebhookHandler, WebhookTriggeredArgs>
    {
        /// <summary>
        /// Initiates the event handling.
        /// </summary>
        /// <param name="content">The body of the request from a Jira webhook POST.</param>
        /// <param name="infoObj">The object which has been linked to the specified Jira issue.</param>
        /// <param name="issueId">The ID of the Jira issue which triggered the webhook.</param>
        public JiraWebhookHandler StartEvent(JObject content, BaseInfo infoObj, string issueId)
        {
            var e = new WebhookTriggeredArgs
            {
                Content = content,
                InfoObject = infoObj,
                Issue = issueId
            };

            return StartEvent(e);
        }
    }
}