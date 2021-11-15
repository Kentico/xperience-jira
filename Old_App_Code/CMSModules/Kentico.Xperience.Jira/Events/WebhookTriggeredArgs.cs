using CMS.Base;
using CMS.DataEngine;
using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.Jira.Events
{
    public class WebhookTriggeredArgs : CMSEventArgs
    {
        /// <summary>
        /// The object which has been linked to the specified Jira issue
        /// </summary>
        public BaseInfo InfoObject
        {
            get;
            set;
        }

        /// <summary>
        /// The body of the request from a Jira webhook POST
        /// </summary>
        public JObject Content
        {
            get;
            set;
        }

        /// <summary>
        /// The ID of the Jira issue which triggered the webhook
        /// </summary>
        public string Issue
        {
            get;
            set;
        }
    }
}