namespace Kentico.Xperience.Jira.Events
{
    /// <summary>
    /// Jira events
    /// </summary>
    public static class JiraEvents
    {
        /// <summary>
        /// Fires when a webhook is received from Jira
        /// </summary>
        public static readonly JiraWebhookHandler WebhookTriggered = new JiraWebhookHandler();
    }
}