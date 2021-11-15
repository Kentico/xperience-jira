using CMS.Base;
using CMS.DataEngine;
using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.Jira.Events
{
    /// <summary>
    /// Jira handler
    /// </summary>
    public class JiraWebhookHandler : AdvancedHandler<JiraWebhookHandler, WebhookTriggeredArgs>
    {
        /// <summary>
        /// Initiates the event handling
        /// </summary>
        /// <param name="content">The body of the request from a Jira webhook POST</param>
        /// <param name="infoObj">The object which has been linked to the specified Jira issue</param>
        /// <param name="issue">The ID of the Jira issue which triggered the webhook</param>
        /// <returns></returns>
        public JiraWebhookHandler StartEvent(JObject content, BaseInfo infoObj, string issue)
        {
            var e = new WebhookTriggeredArgs
            {
                Content = content,
                InfoObject = infoObj,
                Issue = issue
            };

            return StartEvent(e);
        }
    }
}